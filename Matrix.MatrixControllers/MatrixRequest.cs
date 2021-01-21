using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;

namespace Matrix.MatrixControllers
{
    /// <summary>
    /// пакет от матрикса
    /// </summary>
    public class MatrixRequest : IRequestInfo
    {        
        public string Key { get; private set; }

        public int Length { get; private set; }
        public byte Command { get; private set; }        
        public byte[] Body { get; private set; }

        public MatrixRequest(int length, byte command,byte[] body)
        {
            Key = "";
            Length = length;
            Command = command;
            Body = body;
        }
    }
}
