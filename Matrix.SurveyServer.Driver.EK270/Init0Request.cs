using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.EK270
{
    /// <summary>
    /// начальная посылка
    /// </summary>
    class Init0Request : Request
    {
        public string Password { get; private set; }

        public Init0Request(string password)
            : base(RequestType.Read, "", "")
        {
            Password = password;

        }

        public override byte[] GetBytes()
        {
            var bytes = new List<byte>();

            //if (!string.IsNullOrEmpty(Password) && Password != "___") bytes.AddRange(Encoding.ASCII.GetBytes(Password));
            //bytes.AddRange(Encoding.ASCII.GetBytes("00000000"));

            //var bytes1 = new byte[]{    0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,              //                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,             //                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,             //                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,             //                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2F,             //                            0x3F, 0x21, 0x0D, 0x0A
            //};


            //for (var i = 0; i < 8; i++)
            //{
            //    bytes.Add(0x30);
            //}

            int size = 75;
            for (int i = 0; i < size; i++)
            {
                bytes.Add(0x00);
            }

            //bytes.AddRange(Crc.Calc(bytes.ToArray(), 1, bytes.Count - 1, new BccCalculator()).CrcData);
            //return bytes1;//bytes.ToArray();
            return bytes.ToArray();
        }
    }
}
