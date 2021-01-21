using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    class MockConnector : IConnector
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MockConnector));
   
        public bool Relogin()
        {
            log.Debug(string.Format("Relogin"));
            return true;
        }
        public dynamic SendByAPI(dynamic message)
        {
            return null;
        }
        //public void Restart()
        //{
        //    log.Debug(string.Format("Restart"));
        //}

        public void Subscribe()
        {
            log.Debug(string.Format("Subscribe"));
        }

        public dynamic SendMessage(dynamic message)
        {
            log.Debug(string.Format("отправлено сообщение {0}", message.head.what));
            switch ((string)message.head.what)
            {
                case "get-drivers":
                    {
                        string filePath = @"D:\Projects\Matrix\Drivers.js";
                        return File.ReadAllText(filePath);
                    }
                case "get-nodes":
                    {
                        string filePath = @"D:\Projects\Matrix\Nodes.js";
                        
                        return File.ReadAllText(filePath);
                    }
                case "get-records":
                    {
                        string filePath = @"D:\Projects\Matrix\Records.js";
                        return File.ReadAllText(filePath);
                    }
            }
            return null;
        }

        //private MokeConnector()
        //{

        //}
        //private static IConnector instance = null;
        //public static IConnector Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            instance = new MokeConnector();
        //        }
        //        return instance;
        //    }
        //}

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
