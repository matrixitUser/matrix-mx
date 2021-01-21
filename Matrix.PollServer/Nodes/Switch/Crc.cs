//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Matrix.PollServer.Nodes.Switch
//{
//    partial class SwitchNode : BaseNode
//    {
//        public byte CrcCalculate(IEnumerable<byte> bytes)
//        {
//            byte chck = (byte)bytes.Sum(d => d);
//            chck = (byte)((byte)~chck + 1);
//            return chck;
//        }

//        public bool CrcCheck(IEnumerable<byte> bytes)
//        {
//            int Length = bytes.Count();
//            byte crcClc = CrcCalculate(bytes.Take(Length - 1));
//            byte crcMsg = bytes.ElementAt(Length - 1);
//            return crcClc == crcMsg;
//        }
//    }
//}
