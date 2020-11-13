using System;
using System.Net;

namespace HTTPServer
{
    public class Request : GeneralHeader
    {
        public static readonly byte[] Terminator = new byte[] { 13, 10, 13, 10 };
        public string Method;
        public Uri RequestUri;
        public string UserAgent;
        public string Host;
        public IPAddress Originator;

        public Request(string raw, IPAddress origin)
        {
            ConnectionStatus = "close"; // closes request by default.
            ParseHeader(raw);
            Originator = origin;
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
}
