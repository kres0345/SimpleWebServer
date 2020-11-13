using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPServer
{
    class Program
    {
        const int Port = 8080;
        const int REQUEST_TOTAL_TIMEOUT = 10;
        const int REQUEST_TRANSFER_TIMEOUT = 1;

        static async Task Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("10.29.137.144"), Port);
            Console.WriteLine($"Listening on: {tcpListener.LocalEndpoint}");
            tcpListener.Start();

            while (true)
            {
                ThreadPool.QueueUserWorkItem(ClientConnected, await tcpListener.AcceptTcpClientAsync());
            }
        }

        static void ClientConnected(object clientObj)
        {
            Stopwatch totalTimeout = new Stopwatch();
            totalTimeout.Start();

            TcpClient client = clientObj as TcpClient;
            Console.WriteLine("Client connected");
            NetworkStream stream = client.GetStream();

            while (client.Connected)
            {
                Stopwatch dataTransferTimeout = new Stopwatch();

                Queue<byte> requestBytes = new Queue<byte>();
                int endCharsIndex = 0;

                bool requestFinished = false;
                while (!requestFinished)
                {
                    if (!stream.DataAvailable)
                    {
                        if (totalTimeout.Elapsed.TotalSeconds > REQUEST_TOTAL_TIMEOUT || dataTransferTimeout.Elapsed.TotalSeconds > REQUEST_TRANSFER_TIMEOUT)
                        {
                            client.Close();
                            requestFinished = true;
                        }

                        continue;
                    }

                    dataTransferTimeout.Restart();


                    byte newByte = (byte)stream.ReadByte();
                    requestBytes.Enqueue(newByte);

                    if (newByte == Request.Terminator[endCharsIndex])
                    {
                        endCharsIndex++;
                    }
                    else
                    {
                        endCharsIndex = 0;
                    }

                    if (endCharsIndex == 4)
                        break;
                }

                string requestString = Encoding.UTF8.GetString(requestBytes.ToArray());
                
                Request request = new Request(requestString, (client.Client.RemoteEndPoint as IPEndPoint).Address);
                if (request.ContentLength > 0)
                {
                    request.Body = new byte[request.ContentLength];
                    stream.Read(request.Body, 0, request.ContentLength);
                }

                Console.WriteLine($"Recieved request from {request.Originator}\n       -- {request.RequestUri}");

                if (!Handler.HandleRequest(request, out Response response))
                    continue;

                try
                {
                    stream.Write(response.ToBytes());
                }
                catch
                {
                    Console.WriteLine("Failed responding client, closing thread");
                    client.Dispose();
                    return;
                }

                if (request.ConnectionStatus == "close")
                    client.Close();
            }

            client.Dispose();
            Console.WriteLine("Client disconnected");
        }
    }
}
