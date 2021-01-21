using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Ftp
{
    class FtpConnection:PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FtpConnection));


         public FtpConnection(dynamic content)
        {
            this.content = content;
        }

        private string GetDir()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("dir")) return ".";
            return content.dir.ToString();
        }

        public override bool IsFinalNode()
        {
            return true;
        }

        public override int GetPollPriority()
        {
            return 15;
        }

        private bool loop = true;
        
        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            if (!Directory.Exists(GetDir()))
            {
                return Codes.NO_DIRECTORY;
            }



            return Codes.SUCCESS;
        }

        protected override void OnRelease(Route route, int port)
        {
        
        }

        public override string ToString()
        {
            return string.Format("ftp соединение {0}", GetDir());
        }
    }
}
