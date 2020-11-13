using System;

namespace HTTPServer
{
    public static class Handler
    {
        /// <summary>
        /// Handles a request.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <returns>Whether a response is to be sent</returns>
        public static bool HandleRequest(Request req, out Response res)
        {
            switch (req.Method)
            {
                case "get":
                    res = new Response
                    {
                        StatusCode = 420,
                        StatusResponse = "Enhance your calm",
                        Date = DateTime.Now,
                        ConnectionStatus = req.ConnectionStatus,
                        ContentType = "text/html",
                        Content = "<span>Hello world</span>"
                    };

                    return true;
                default:
                    break;
            }

            res = new Response();
            return true;
        }
    }
}
