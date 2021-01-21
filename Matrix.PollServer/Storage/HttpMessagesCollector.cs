using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Storage
{
    class HttpMessagesCollector
    {
        public void PushMessage(dynamic message)
        {

        }

        private HttpMessagesCollector() { }
        static HttpMessagesCollector() { }
        private static readonly HttpMessagesCollector instance = new HttpMessagesCollector();
        public static HttpMessagesCollector Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
