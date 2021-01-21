//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SPG761.Protocol
//{
//    class Head : IBytes
//    {
//        public const byte SOH = 0x01;
//        public const byte ISI = 0x1F;

//        public byte? Address { get; set; }

//        private byte fnc;

//        public FunctionCode FNC
//        {
//            get
//            {
//                return (FunctionCode)fnc;
//            }
//            set
//            {
//                fnc = (byte)value;
//            }
//        }

//        public byte[] DataHead { get; set; }

//        public string Text
//        {
//            get
//            {
//                var encoding = Encoding.GetEncoding(866);
//                return encoding.GetString(DataHead.ToArray());
//            }
//            set
//            {
//                var encoding = Encoding.GetEncoding(866);
//                DataHead = encoding.GetBytes(value);
//            }
//        }

//        public IEnumerable<byte> GetBytes()
//        {
//            List<byte> bytes = new List<byte>();
//            bytes.Add(Message.DLE);
//            bytes.Add(SOH);

//            if (Address.HasValue)
//            {
//                //
//                bytes.Add(0);
//                bytes.Add(Address.Value);
//            }

//            bytes.Add(Message.DLE);
//            bytes.Add(ISI);
//            bytes.Add(fnc);

//            if (DataHead != null)
//            {
//                foreach (var dh in DataHead)
//                {
//                    bytes.Add(dh);
//                    if (dh == Message.DLE) bytes.Add(dh);
//                }
//            }


//            return bytes;
//        }

//        public static Head Parse(byte[] data, int offset, int length)
//        {
//            if (data == null) return null;
//            if (data.Length < offset + length) return null;
//            if (data.Length <= offset + 5) return null;

//            var FNC = data[offset + 4];
//            var dataHead = data.Skip(offset + 5).Take(length - 5).ToArray();

//            var undle = new List<byte>();
//            for (var i = 0; i < dataHead.Length; i++)
//            {
//                var b = dataHead[i];
//                if (b == Message.DLE) i++;

//                undle.Add(b);
//            }

//            return new Head()
//            {
//                FNC = (FunctionCode)FNC,
//                DataHead = undle.ToArray()
//            };
//        }
//    }
//}
