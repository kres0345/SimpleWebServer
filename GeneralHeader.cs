using System;

namespace HTTPServer
{
    public class GeneralHeader
    {
        public int ContentLength;
        public byte[] Body;
        public string ConnectionStatus; // 'close', 'keep-alive' etc.
        public DateTime Date;
    }
}
