using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SuperSocket.SocketBase.Protocol;

namespace Matrix.MatrixControllers
{
    public class MatrixReceiveFilter : IReceiveFilter<MatrixRequest>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private byte[] buffer = new byte[1024 * 1024];
        private int lastOffset = 0;

        public MatrixRequest Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            var buffer = new byte[length];
            Array.Copy(readBuffer, offset, buffer, 0, length);

            byte command = 0;
            var body = buffer.ToArray();

            if (length < 4)
            {
                rest = 0;
                logger.Debug("длина пакета меньше 4 байт ({0} байт), обнуление", length);
                return null;
            }

            //определение длины пакета (либо 1 первый байт, либо 2 байта (2 и 3))
            ushort len = buffer[0];
            if (len == 0)
            {
                len = BitConverter.ToUInt16(buffer, 1);
                command = buffer[3];
                body = buffer.Skip(4).Take(len - (4 + 2)).ToArray();
            }
            else
            {
                command = buffer[1];
                body = buffer.Skip(2).Take(len - (2 + 2)).ToArray();
            }

            if (length < len)
            {
                rest = 0;
                logger.Debug("длина пакета ({0} байт) меньше указанной в поле len ({1} байт), ожидание следующей порции данных", length, len);
                return null;
            }

            if (length > len)
            {
                rest = length - len;
                logger.Debug("длина пакета ({0} байт) больще указанной в поле len ({1} байт), остаток", length, len);
            }

            rest = 0;
            return new MatrixRequest(length: len, command: command, body: body);
        }

        public void Reset()
        {

        }

        public IReceiveFilter<MatrixRequest> NextReceiveFilter { get; private set; }
        public int LeftBufferSize { get; private set; }
        public FilterState State { get; private set; }

      
    }
}
