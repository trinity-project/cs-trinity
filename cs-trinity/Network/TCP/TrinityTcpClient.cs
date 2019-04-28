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
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trinity;

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

        public TrinityTcpClient(string ip, string port)
        {
            serverIp = ip;
            serverPort = port;
            messageQueue = new ConcurrentQueue<string>();
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
                byte[] buffer = new byte[bufferSize];
                buffer = Encoding.UTF8.GetBytes(msg);
                clientSocket.Send(buffer);               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private UInt16 GetMessageCount(byte[] buffer, int recvLength)
        {
            // Buffer has data and the received length is lager than zero
            if (null != buffer && 0 < recvLength) {
                // convert the message to the utf-8 string
                string msg = Encoding.UTF8.GetString(buffer, 0, recvLength);

            }
            return 0;
        }

        private void UnWrapMessageToAdaptGateway(byte[] buffer, int recvLength, ref List<string> msgList)
        {
            //msgList = default(List<string>);
            
            try
            {
                if (null == buffer || 0 >= recvLength)
                {
                    // TODO: record to log file later
                    Console.WriteLine("Terminate unwrapping the message from gateway. Length={0}", recvLength);
                    return;
                }

                // start parse the recevied data
                int splitLength = 0;
                string msg = null;
                // Message without Header wrapped by gateway
                if (123 == buffer[0])
                {
                    msg = Encoding.UTF8.GetString(buffer, 0, recvLength);
                    splitLength = 0;
                }
                else // Message with Header wrapped by gateway
                {
                    int msgLength = buffer.Skip(4).Take(4).ToArray().ToInt32();
                    splitLength = msgLength + 12;
                    msg = Encoding.UTF8.GetString(buffer.Skip(12).Take(msgLength).ToArray(), 0, msgLength);
                }

                msgList.Add(msg);

                int newMsgLength = recvLength - splitLength;
                this.UnWrapMessageToAdaptGateway(
                    buffer.Skip(splitLength).Take(newMsgLength).ToArray(), newMsgLength, ref msgList);
            }
            catch (Exception Expinfo)
            {
                // Record this exception to file in the future
                Console.WriteLine("Failed to unwrap messages: {}", Expinfo);
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
                    /*
                    if (client == null)
                    {
                        continue;
                    }
                    NetworkStream clientStream = client.GetStream();
                    byte[] buffer = new byte[bufferSize];
                    MemoryStream reponseMessage = new MemoryStream();
                    int readBytes = 1;
                    while (readBytes > 0)
                    {
                        readBytes = clientStream.Read(buffer, 0, bufferSize);
                        reponseMessage.Write(buffer, 0, readBytes);
                    }
                    //clientStream.Close();

                    byte[] message = reponseMessage.GetBuffer();
                    string str = Encoding.UTF8.GetString(message);
                    Form_main.showInformation((str.Length).ToString());
                    if (str.Length > 0)
                    {
                        //messageQueue.Enqueue(str);
                    }
                    */
                    //Console.WriteLine("recieved message");
                    //byte[] buffer = new byte[bufferSize];
                    buffer.Initialize();
                    //string str = null;
                    //实际接收到的有效字节数
                    //int len = 0;
                    /*
                    try
                    { 

                        do
                        {
                            len = client.Receive(buffer);
                            if (Encoding.UTF8.GetString(buffer, 0, len).Equals("eof"))
                            {
                                break;
                            }
                            Console.WriteLine("read {0} data", len.ToString());
                            str += Encoding.UTF8.GetString(buffer, 0, len);
                            //Console.WriteLine("read {0} data", len.ToString());
                        } while (len > 0);//while(client.Poll(500, SelectMode.SelectRead) && client.Available > 0 && client.Connected);      
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine("read exception");
                    }*/

                    int len = clientSocket.Receive(buffer);
                    if (0 >= len)
                    {
                        continue;
                    }

                    msgArray.Clear();
                    this.UnWrapMessageToAdaptGateway(buffer, len, ref msgArray);

                    foreach (string msg in msgArray) {
                        if (msg.Contains(expected))
                        {
                            VerificationResult = true;
                            Console.WriteLine("Received {0}: {1}", expected, msg);
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
                    Console.WriteLine(ex.ToString());
                }
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
    }
}
