using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        public byte[] MakeBaseRequest0X03(UInt16 u16AddressForReading, UInt16 u16CountOfRegistersForReading)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(NetworkAddress);

            bytes.Add(0x03);

            bytes.AddRange(BitConverter.GetBytes(u16AddressForReading).Reverse());                    //Нач-ый адрес для чтения

            bytes.AddRange(BitConverter.GetBytes(u16CountOfRegistersForReading).Reverse());           //Количество регистров для чтения
          
            byte lrc = LRC.Lrc(bytes.ToArray(), bytes.Count);
            bytes.Add(lrc);

            List<byte> sendBytes = new List<byte>();

            sendBytes.AddRange(Encoding.ASCII.GetBytes(string.Format(":{0}", string.Join("", bytes.Select(b => b.ToString("X2"))))));

            sendBytes.Add(0x0D); sendBytes.Add(0x0A); //перевод строки

            return sendBytes.ToArray();
        }
    }
}
