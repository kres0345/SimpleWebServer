using System.Text;

namespace HTTPServer
{
    public class Response : GeneralHeader
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
