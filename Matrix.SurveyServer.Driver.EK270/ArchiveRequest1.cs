using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.EK270
{
    class ArchiveRequest1 : Request
    {
        public const byte SOH = 0x01;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;

        public int ArchiveNumber { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public int Count { get; private set; }

        public ArchiveRequest1(int archiveNumber, DateTime start, DateTime end, int count)
            : base(RequestType.Read, "", "")
        {
            ArchiveNumber = archiveNumber;
            Start = start;
            End = end;
            Count = count;
        }

        public override byte[] GetBytes()
        {
            var e = Encoding.ASCII;

            var bytes = new List<byte>();
            bytes.Add(SOH);
            bytes.AddRange(e.GetBytes("R3"));
            bytes.Add(STX);
            var parameters = string.Format("{0}:V.{1}({2};{3:yyyy-MM-dd,HH:mm:ss};{4:yyyy-MM-dd,HH:mm:ss};{5})", ArchiveNumber, 0, 3, Start, End, Count);
            //var parameters = string.Format("{0}:V.{1}({2};{3:yyyy-MM-dd,HH:mm:ss};;{5})", ArchiveNumber, 0, 3, Start, End, Count);
            bytes.AddRange(e.GetBytes(parameters));
            bytes.Add(ETX);
            bytes.AddRange(Crc.Calc(bytes.ToArray(), 1, bytes.Count - 1, new BccCalculator()).CrcData);
            return bytes.ToArray();
        }
    }
}
