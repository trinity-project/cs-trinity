/*
Author: Trinity Core Team

MIT License

Copyright (c) 2018 Trinity

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trinity;
using Trinity.TrinityDB;



namespace Trinity.Network.TCP
{
    public class TrinityTcpClient
    {
        private TcpClient tcpClient;
        private const int bufferSize = 1024;
        private string serverIp;
        private string serverPort;
        private ConcurrentQueue<string> messageQueue;

        // for parse the messages from the gateway temperorily. Should be changed later
        private readonly string messageHeaderStartString;
        private readonly string messageHeaderEndString;

        private readonly byte[] messageHeaderStartBytes;
        private readonly byte[] messageHeaderEndBytes;

        private readonly int messageVersion;
        private readonly int messageEnd;
        private readonly int messageHeaderLength;

        public TrinityTcpClient(string ip, string port)
        {
            serverIp = ip;
            serverPort = port;
            messageQueue = new ConcurrentQueue<string>();

            this.messageVersion = 0x1;
            this.messageEnd = 0x65;
            this.messageHeaderLength = 12;

            this.messageHeaderStartBytes = this.ConvertHeaderContent(this.messageVersion);
            this.messageHeaderStartString = this.messageHeaderStartBytes.ToStringUtf8();

            this.messageHeaderEndBytes = this.ConvertHeaderContent(this.messageEnd);
            this.messageHeaderEndString = this.messageHeaderEndBytes.ToStringUtf8();
        }

        public void CreateConnetion()
        {
            try
            {
                this.tcpClient = new TcpClient();
                IPEndPoint EPServer = new IPEndPoint(IPAddress.Parse(this.serverIp), Convert.ToInt32(this.serverPort));
                this.tcpClient.Connect(EPServer);

                // Set the socket with alive
                this.tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                this.tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);


                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)90000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));  //首次探测时间90秒 
                BitConverter.GetBytes((uint)60000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2); // 间隔侦测时间60秒
                this.tcpClient.Client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            }
            catch (Exception ExcpInfo)
            {
                Log.Fatal("Failed to create tcp client. Exception: {0}", ExcpInfo);
            }
        }

        public Socket GetConnection()
        {
            return this.tcpClient.Client;
        }

        public void SendData(string msg)
        {
            try
            {
                if (0 >= msg.Length)
                {
                    throw new Exception(string.Format("Error Message Length {0}", msg.Length));
                }
                
                byte[] buffer = msg.ToBytesUtf8();
                byte[] messageBytes = this.messageHeaderStartBytes
                    .Concat(this.ConvertHeaderContent(buffer.Length))
                    .Concat(this.messageHeaderEndBytes).Concat(buffer).ToArray();

                int sendLength = messageBytes.Length;
                int sent = 0;
                int offset = 0;
                while (0 < sendLength)
                {
                    sent = this.tcpClient.Client.Send(messageBytes, offset, sendLength, SocketFlags.None);
                    offset += sent;
                    sendLength -= sent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void UnWrapMessageFromGateway(byte[] buffer, int recvLength)
        {
            // Decode the message from the Gateway
            try
            {
                int msgLength = this.DecodeMessageHeader(buffer, recvLength);
                string msg = buffer.Skip(this.messageHeaderLength).Take(msgLength).ToArray().ToStringUtf8();
                if (null == msg)
                {
                    Log.Error("Failed to parse the message: {0} from gateway", buffer.ToStringUtf8());
                    return;
                }

                // Add message to message queue
                this.messageQueue.Enqueue(msg);

                // to judge whether there are others messages or not
                msgLength += this.messageHeaderLength;
                if (msgLength + this.messageHeaderLength >= recvLength)
                {
                    // only one message existed in the buffer
                    return;
                }
                else
                {
                    // parse the next message from the buffer
                    this.UnWrapMessageFromGateway(buffer.Skip(msgLength).ToArray(), recvLength - msgLength);
                }
            }
            catch (Exception ExpInfo)
            {
                Log.Warn("Exception occured during parse message:  {0} from gateway. Exception: {1}",
                    (null != buffer) ? buffer.ToStringUtf8() : "null", ExpInfo);
            }
        }

        private int DecodeMessageHeader(byte[] buffer, int recvLength)
        {
            if (null == buffer || this.messageHeaderLength >= recvLength)
            {
                Log.Warn("Terminate unwrapping the message from gateway. Length={0}", recvLength);
                throw new Exception("Null Messages");
            }

            // parse version
            int version = BitConverter.ToInt32(buffer.Take(4).Reverse().ToArray(), 0);
            if (this.messageVersion != version)
            {
                Log.Warn("Message with error version. {0}", version);
                throw new Exception(string.Format("DROP: Message with invalid version: {0}", version));
            }

            int end = BitConverter.ToInt32(buffer.Skip(8).Take(4).Reverse().ToArray(), 0);
            if (this.messageEnd != end)
            {
                Log.Error("Message with error end bytes {0}", end);
                throw new Exception(string.Format("DROP: Message with Error end of flag: {0}.", end));
            }

            int length = BitConverter.ToInt32(buffer.Skip(4).Take(4).Reverse().ToArray(), 0);
            if (length + this.messageHeaderLength > recvLength)
            {
                Log.Error("Message with error length: {0}, receive length: {1}", length, recvLength);
                throw new Exception(string.Format("DROP: Message with error length: {0}", length));
            }

            return length;
        }

        private int Receive(out byte[] message)
        {
            message = new byte[0];
            byte[] buffer = new byte[bufferSize];
            int recvLength = 0;
            int totalReceived = 0;
            int readSize = bufferSize;

            // read the data until end each time.
            while (true)
            {
                // set buffer zero
                buffer.Initialize();

                // Read the message header firstly
                recvLength = this.tcpClient.Client.Receive(buffer, readSize, SocketFlags.None);
                if (0 >= recvLength)
                {
                    return -1;
                }
                else if (bufferSize > recvLength)
                {
                    totalReceived += recvLength;
                    message = message.Concat(buffer.Take(recvLength)).ToArray();

                    // parse this message header firstly
                    if (message.Length > this.messageHeaderLength)
                    {
                        int expectedLength = BitConverter.ToInt32(message.Skip(4).Take(4).Reverse().ToArray(), 0);
                        if (expectedLength + this.messageHeaderLength == totalReceived)
                        {
                            break;
                        }
                        else if (expectedLength + this.messageHeaderLength > totalReceived)
                        {
                            readSize = expectedLength + this.messageHeaderLength - totalReceived;
                            readSize = readSize <= bufferSize ? readSize : bufferSize;
                        }
                        else
                        {
                            // means a new message is received
                            Log.Warn("Should never go here. Why???");
                            int msgLength = expectedLength + this.messageHeaderLength;
                            this.UnWrapMessageFromGateway(buffer.Take(msgLength).ToArray(), msgLength);

                            message = message.Skip(msgLength).ToArray();
                        } 
                    }
                }
                else
                {
                    totalReceived += bufferSize;
                    message = message.Concat(buffer).ToArray();
                    continue;
                }
            }

            return totalReceived;
        }

        public bool ReceiveMessage(string expected = "MessageTypeNotSetForVerification")
        {
            bool VerificationResult = false;
            int recvLength = 0;

            // start receive the messages
            while (true)
            {
                try
                {
                    recvLength = this.Receive(out byte[] messages);
                    if (0 >= recvLength)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    this.UnWrapMessageFromGateway(messages, recvLength);

                    // for test method: break this while loop
                    if (VerificationResult)
                    {
                        break;
                    }

                }
                catch (Exception ExpInfo)
                {
                    //Log.Error("Exception occurred during receive message. Exception: {0}", ExpInfo);
                }

                Thread.Sleep(200);
            }

            return VerificationResult;
        }

        private void HandleMessage()
        {
            while (true)
            {
                try
                {
                    string message;
                    if (!messageQueue.IsEmpty)
                    {
                        if (messageQueue.TryDequeue(out message))
                        {
                            //string str = message.ToString();
                            Console.WriteLine("HandleMessage: {}", message);
                            JObject rb = (JObject)JsonConvert.DeserializeObject(message);
                            Console.WriteLine(rb["MessageBody"]["Deposit"]);
                            // TODO:triger api to handle message;
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public bool GetMessageFromQueue(out string message)
        {

            if (!messageQueue.IsEmpty)
            {
                return messageQueue.TryDequeue(out message);
            }
            else
            {
                message = null;
                return false;
            }
        }

        public void CloseConnection()
        {
            try
            {
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private byte[] ConvertHeaderContent(int content)
        {
            return BitConverter.GetBytes(content).Reverse().ToArray();
        }
    }
}
