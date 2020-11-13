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
                
                Request request = new Request(requestString);
                if (request.ContentLength > 0)
                {
                    request.Body = new byte[request.ContentLength];
                    stream.Read(request.Body, 0, request.ContentLength);
                }

                Console.WriteLine($"Requested page: {request.RequestUri} ---");
                Console.WriteLine(request.UserAgent);

                Response response = new Response
                {
                    StatusCode = 420,
                    StatusResponse = "Enhance your calm",
                    Date = DateTime.Now,
                    ConnectionStatus = request.ConnectionStatus,
                    ContentType = "text/html",
                    Content = "<span>Hello world</span>"
                };

                try
                {
                    stream.Write(response.ToBytes());
                }
                catch
                {
                    Console.WriteLine("Failed responding client, closing thread");
                    return;
                }

                if (request.ConnectionStatus == "close")
                    client.Close();
            }

            client.Dispose();
            Console.WriteLine("Client disconnected");
        }

        static void HandleRequest(Request req)
        {
            switch (req.Method)
            {
                case "get":

                    break;
                default:
                    break;
            }
        }

        class GeneralFields
        {
            public int ContentLength;
            public byte[] Body;
            public string ConnectionStatus; // 'close', 'keep-alive' etc.
            public DateTime Date;
        }

        class Request : GeneralFields
        {
            public static readonly byte[] Terminator = new byte[] { 13, 10, 13, 10 };
            public string Method;
            public Uri RequestUri;
            public string UserAgent;
            public string Host;

            public Request(string raw)
            {
                ConnectionStatus = "close"; // closes request by default.
                ParseHeader(raw);
            }

            public void ParseHeader(string rawHeader)
            {
                string[] lines = rawHeader.Split("\r\n");

                foreach (var line in lines)
                {
                    // Check if this is the request-line
                    if (line.ToLower().EndsWith("http/1.1"))
                    {
                        string[] words = line.Split(' ');

                        Method = words[0].ToLower();
                        RequestUri = new Uri(words[1], UriKind.RelativeOrAbsolute);
                        continue;
                    }

                    string[] keyValuePair = line.Split(':', 2);

                    if (keyValuePair.Length == 1)
                        continue;
                    
                    switch (keyValuePair[0].ToLower())
                    {
                        case "user-agent":
                            UserAgent = keyValuePair[1];
                            break;
                        case "host":
                            Host = keyValuePair[1];
                            break;
                        case "content-length":
                            if (int.TryParse(keyValuePair[1].Trim(), out int res))
                                ContentLength = res;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        class Response : GeneralFields
        {
            public int StatusCode;
            public string StatusResponse;
            public string ContentType;
            public string Content;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", StatusCode, StatusResponse);
                sb.AppendFormat("Date: {0}\r\n", Date.ToString("ddd, dd MMM yyyy T"));
                sb.AppendFormat("Connection: {0}\r\n", ConnectionStatus);
                sb.AppendFormat("Content-Length: {0}\r\n", Content.Length);
                sb.AppendFormat("Content-Type: {0}\r\n", ContentType);
                sb.AppendLine();
                sb.Append(Content);

                return sb.ToString();
            }

            public byte[] ToBytes() => Encoding.UTF8.GetBytes(ToString());
        }
    }
}
