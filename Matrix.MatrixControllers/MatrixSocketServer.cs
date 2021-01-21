using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace Matrix.MatrixControllers
{
    public class MatrixSocketServer : AppServer<MatrixSession, MatrixRequest>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MatrixSocketServer()
            : base(new DefaultReceiveFilterFactory<MatrixReceiveFilter, MatrixRequest>())
        {            
        }
    }
}
