using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        public byte[] MakeBaseRequest0X48(UInt16 u16AddressForReading, UInt16 u16CountOfRegistersForReading, UInt16 u16AddressForWriting, UInt16 u16CountOfRegistersForWrit, UInt16 u16CountOfBytesForWrit, UInt16 u16RequestNumber, byte[] registersForWrit = null)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(NetworkAddress);

            bytes.Add(0x48);

            bytes.AddRange(BitConverter.GetBytes(u16AddressForReading).Reverse());                    //Нач-ый адрес для чтения

            bytes.AddRange(BitConverter.GetBytes(u16CountOfRegistersForReading).Reverse());           //Количество регистров для чтения

            bytes.AddRange(BitConverter.GetBytes(u16AddressForWriting).Reverse());                    //Нач-ый адрес для записи

            bytes.AddRange(BitConverter.GetBytes(u16CountOfRegistersForWrit).Reverse());              //Количество регистров для записи

            bytes.AddRange(BitConverter.GetBytes(u16CountOfBytesForWrit).Reverse());                  //Кол-во байт для записи

            bytes.AddRange(BitConverter.GetBytes(u16RequestNumber).Reverse());                        //Номер запроса

            if (u16CountOfRegistersForWrit != 0)
            {         
                bytes.AddRange(registersForWrit);                   //1,2,..,N-ый регистр для записи
            }

            byte lrc = LRC.Lrc(bytes.ToArray(), bytes.Count);
            bytes.Add(lrc);

            List<byte> sendBytes = new List<byte>();

            sendBytes.AddRange(Encoding.ASCII.GetBytes(string.Format(":{0}", string.Join("", bytes.Select(b => b.ToString("X2"))))));

            sendBytes.Add(0x0D); sendBytes.Add(0x0A); //перевод строки

            return sendBytes.ToArray();
        }
    }
}
