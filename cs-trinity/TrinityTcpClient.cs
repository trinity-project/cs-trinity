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

namespace Wallet
{
    class TrinityTcpClient
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

        public void createConnetion()
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
                Thread receiveThread = new Thread(receiveMessage);
                Thread handleMsgThread = new Thread(handleMessage);
                receiveThread.IsBackground = true;
                handleMsgThread.IsBackground = true;
                receiveThread.Start();
                handleMsgThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void sendData(string msg)
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

        private void receiveMessage()
        {
            
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
                    byte[] buffer = new byte[bufferSize];
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
                    string msg = Encoding.UTF8.GetString(buffer, 0, len);
                    //Console.WriteLine(msg);
                    messageQueue.Enqueue(msg);
                    //Console.WriteLine("get message : {0}", Encoding.UTF8.GetString(buffer, 0, len));
                    //receicedData = (client.RemoteEndPoint + ":" + str);
                    //Form_main.showInformation(receicedData);

                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            }
        }

        private void handleMessage()
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
                            Console.WriteLine(message);
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

        private void closeConnection()
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
