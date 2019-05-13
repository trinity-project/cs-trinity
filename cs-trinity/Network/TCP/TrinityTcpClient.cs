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
        private Socket clientSocket;
        //private TcpClient client = null;
        private const int bufferSize = 1024 * 1024 * 3;
        //private string receicedData;
        //private Thread clientThread;
        private string serverIp;
        private string serverPort;
        private ConcurrentQueue<string> messageQueue;

        // for parse the messages from the gateway temperorily. Should be changed later
        private readonly string messageHeaderStart;
        private readonly string messageHeaderEnd;

        public TrinityTcpClient(string ip, string port)
        {
            serverIp = ip;
            serverPort = port;
            messageQueue = new ConcurrentQueue<string>();

            this.messageHeaderStart = this.ConvertHeaderContent(0x1);
            this.messageHeaderEnd = this.ConvertHeaderContent(0x65);
        }

        public void CreateConnetion()
        {
            try
            {
                /*
                client = new TcpClient();
                IPAddress IP = IPAddress.Parse(serverIp);
                IPEndPoint point = new IPEndPoint(IP, Convert.ToInt32(serverPort));
                client.Connect(point);
                */


                //创建客户端Socket，获得远程ip和端口号
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress IP = IPAddress.Parse(serverIp);
                IPEndPoint point = new IPEndPoint(IP, Convert.ToInt32(serverPort));

                clientSocket.Connect(point);
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);

                
                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)90000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));  //首次探测时间90秒 
                BitConverter.GetBytes((uint)60000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2); // 间隔侦测时间60秒
                clientSocket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                

                //开启新的线程，不停的接收服务器发来的消息
                //Thread receiveThread = new Thread(ReceiveMessage);
                //Thread handleMsgThread = new Thread(HandleMessage);
                //receiveThread.IsBackground = true;
                //handleMsgThread.IsBackground = true;
                //receiveThread.Start();
                //handleMsgThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public Socket GetConnection()
        {
            return this.clientSocket;
        }

        public void SendData(string msg)
        {
            try
            {
                if (0 >= msg.Length)
                {
                    throw new Exception(string.Format("Error Message Length {0}", msg.Length));
                }
                
                byte[] buffer = new byte[msg.Length + 12];

                string msgWithHeader = this.messageHeaderStart + this.ConvertHeaderContent(msg.Length) + this.messageHeaderEnd + msg;
                buffer = Encoding.UTF8.GetBytes(msg);
                clientSocket.Send(buffer);               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void UnWrapMessageToAdaptGateway(string messages, ref List<string> msgList)
        {
            try
            {
                if (null == messages || 0 >= messages.Length)
                {
                    // TODO: record to log file later
                    Console.WriteLine("Terminate unwrapping the message from gateway. Length={0}", messages.Length);
                    return;
                }

                // start parse the recevied data
                if (-1 == messages.IndexOf(this.messageHeaderStart))
                {
                    int splitPosition = messages.IndexOf("}{");
                    // not include the Gateway Message Header. Simply split the message by "}{"
                    if (-1 == splitPosition)
                    {
                        msgList.Add(messages);
                    }
                    else
                    {
                        msgList.Add(messages.Substring(0, splitPosition + 1));
                        this.UnWrapMessageToAdaptGateway(messages.Substring(splitPosition + 1, messages.Length), ref msgList);
                    }
                }
                else
                {
                    int startPosition = messages.IndexOf(this.messageHeaderStart);
                    int endPosition = messages.IndexOf(this.messageHeaderEnd);

                    // valid messages only satisfied below conditions
                    if (0 == startPosition && 8 == endPosition)
                    {
                        string msg = messages.Substring(12);
                        int splitPosition = msg.IndexOf(this.messageHeaderStart);

                        if (-1 == splitPosition)
                        {
                            // Just only one messages
                            msgList.Add(msg);
                        }
                        else
                        {
                            if (splitPosition >= messages.Length)
                            {
                                return;
                            }
                            msgList.Add(msg.Substring(0, splitPosition));
                            this.UnWrapMessageToAdaptGateway(msg.Substring(splitPosition), ref msgList);
                        }
                    }
                }
            }
            catch (Exception Expinfo)
            {
                // Record this exception to file in the future
                Log.Fatal("Failed to unwrap messages: {0}. Exception: {1}", messages, Expinfo);
            } 
        }

        private int GetMessageLength(byte[] length)
        {
            if (null != length)
            {
                return 0;
            }

            return 0;
        }

        public bool ReceiveMessage(string expected="MessageTypeNotSetForVerification")
        {
            bool VerificationResult = false;
            byte[] buffer = new byte[bufferSize];
            List<string> msgArray = new List<string>();

            while (true)
            {
                try
                {
                    // set buffer zero
                    buffer.Initialize();

                    int len = clientSocket.Receive(buffer);
                    if (0 >= len)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    msgArray.Clear();
                    this.UnWrapMessageToAdaptGateway(Encoding.UTF8.GetString(buffer, 0, len), ref msgArray);

                    // Go through the message list
                    foreach (string msg in msgArray) {
                        if (msg.Contains(expected))
                        {
                            VerificationResult = true;
                            Log.Debug("Received {0}: {1}", msg, expected);
                            break;
                        }
                        messageQueue.Enqueue(msg);
                    }

                    // for test method: break this while loop
                    if (VerificationResult)
                    {
                        break;
                    }

                    // Here we get the index of "{" and remove message header wrapped by gateway.
                    // string msg = Encoding.UTF8.GetString(buffer, buffer.ToList().IndexOf(123), len);
                    
                    //Console.WriteLine("get message : {0}", Encoding.UTF8.GetString(buffer, 0, len));
                    //receicedData = (client.RemoteEndPoint + ":" + str);
                    //Form_main.showInformation(receicedData);
                    Thread.Sleep(1000);

                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }

                Thread.Sleep(1000);
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
                clientSocket.Close();
                //clientThread.Abort();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string ConvertHeaderContent(int content)
        {
            return BitConverter.GetBytes(content).Reverse().ToArray().ToStringUtf8();
        }
    }
}
