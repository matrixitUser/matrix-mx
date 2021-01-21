using System;
using System.Collections.Generic;
using System.Linq;

namespace Matrix.SurveyServer.Driver.CE102
{
    public static class DriverHelper
    {
        private const byte OPT = 0x48;
        private const byte END = 0xC0;
        private const byte ScreeningEND = 0xDC;
        private const byte ESC = 0xDB;
        private const byte ScreeningESC = 0xDD;
        private const byte ClassAccess = 0x50;
        private const byte Direct = 0x80;

        private const byte ClassAccessMask = 0x70;
        private const byte DataLengthMask = 0x0F;


        #region CreateMessage
        public static byte[] CreateMessage(int address, int selfAddress, IEnumerable<byte> pal)
        {
            List<byte> res = new List<byte>(pal);

            InsertInt(res, (UInt16)selfAddress, 0);
            InsertInt(res, (UInt16)address, 0);
            res.Insert(0, OPT);
            ComputeChecksum(res);

            InsertEscSymbol(res);

            res.Insert(0, END);
            res.Add(END);

            return res.ToArray();
        }

        public static byte[] CreatePal(UInt16 command, IEnumerable<byte> data, string password)
        {
            List<byte> res = new List<byte>(data);
            InsertInt(res, command, 0, true);
            res.Insert(0, CreateSerf((byte)data.Count()));
            res.InsertRange(0, ConvertPassword(password, 4));

            return res.ToArray();
        }

        public static void InsertInt(List<byte> list, UInt16 value, UInt16 position, bool needReverse = false)
        {
            var mas = BitConverter.GetBytes(value);
            if (needReverse)
                Array.Reverse(mas);

            list.InsertRange(position, mas);
        }

        public static byte CreateSerf(byte dataLength)
        {
            byte res = 0;

            res = (byte)(res ^ Direct);
            res = (byte)(res ^ ClassAccess);
            var d = dataLength << 4 >> 4;//длина сообщеиния должна занимать только 4 бита
            res = (byte)(res ^ dataLength);

            return res;
        }
        public static byte[] ConvertPassword(string password, int maxLength)
        {
            byte[] res = new byte[maxLength];

            if (password != null)
            {
                int indexPassword = password.Length;
                for (int i = res.Length - 1; i >= 0; i--)  //прижимаем вправо
                {
                    indexPassword = indexPassword - 2;
                    if (indexPassword >= 0)
                    {

                        res[i] = System.Convert.ToByte(password.Substring(indexPassword, 2), 16);
                    }
                    else
                        if (indexPassword == -1)
                        {
                            res[i] = System.Convert.ToByte(password.Substring(0, 1), 16);

                            return res;
                        }

                }
            }

            return res; 
        }
        #endregion

        public static byte[] CreateTestMessage(int address, int selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(1, new List<byte>(), password));
        }

        #region Current
        internal static byte[] ReadCurrentPower(int address, int selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0132, new List<byte>(), password));
        }
        internal static byte[] ReadAveragePower(int address, int selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x012E, new List<byte>(), password));
        }
        internal static byte[] ReadRelayState(int address, int selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x011C, new List<byte>(), password));
        }
        internal static byte[] ReadDateTime(int address, int selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0120, new List<byte>(), password));
        }

        #endregion


        #region Constants
        internal static byte[] ReadVersion(int address, byte selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0100, new List<byte>(), password));
        }
        internal static byte[] ReadVersionEx(int address, byte selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0100, new List<byte> { 0 }, password));
        }
        internal static byte[] ReadConfig(int address, byte selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0101, new List<byte>(), password));
        }
        internal static byte[] ReadTarProg(int address, byte selfAddress, string password, byte type, byte changePoint, byte countPoint)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0140, new List<byte> { type, changePoint, countPoint }, password));
        }
        internal static byte[] ReadSubscriberNumber(int address, byte selfAddress, string password, int partNumber)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x0118, new List<byte> { (byte)partNumber }, password));
        }
        internal static byte[] ReadCurTarrif(int address, byte selfAddress, string password, int tariffNumber)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x012d, new List<byte> { (byte)tariffNumber }, password));
        }
        internal static byte[] ReadSerialNumber(int address, byte selfAddress, string password, int partNumber)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x011a, new List<byte> { (byte)partNumber }, password));
        }
        internal static byte[] ReadLastHalfHourEnergy(int address, byte selfAddress, string password)
        {
            return CreateMessage(address, selfAddress, CreatePal(0x011a, new List<byte>(), password));
        }
        #endregion

        #region Archive
        internal static byte[] ReadEnergyOfInterval(int address, int selfAddress, string password, DateTime date, int intervalNumber, int valueVCount)
        {
            var par = new List<byte>
                          {
                              ParserHelper.ToBcd(date.Day),
                              ParserHelper.ToBcd(date.Month),
                              ParserHelper.ToBcd(date.Year - 2000),
                              ParserHelper.ToBcd(intervalNumber),
                              ParserHelper.ToBcd(valueVCount)
                          };
            return CreateMessage(address, selfAddress, CreatePal(0x0134, par, password));
        }

        /// <summary>
        /// Чтение суммарных значений энергии по тарифам, сохраненных на конец суток, за прошедшие 45 суток
        /// </summary>
        /// <param name="address"></param>
        /// <param name="selfAddress"></param>
        /// <param name="password"></param>
        /// <param name="date"></param>
        /// <param name="dayBack">За сколько суток назад.  Значение -  от 1 до 45 (суток назад).  </param>
        /// <returns></returns>
        internal static byte[] ReadEnergyOfDay(int address, int selfAddress, string password, DateTime date, int dayBack)
        {
            return CreateMessage(address, selfAddress,
                                 CreatePal(0x012f, new List<byte> { ParserHelper.ToBcd(dayBack) }, password));
        }
        internal static byte[] ReadEnergyOfMonth(int address, int selfAddress, string password, DateTime date, int monthBack)
        {
            return CreateMessage(address, selfAddress,
                                 CreatePal(0x0131, new List<byte> { ParserHelper.ToBcd(monthBack) }, password));
        }

        #endregion
        #region EventJournal

        internal static byte[] ReadJournal(int address, int selfAddress, string password, int eventNumber, int journalNumber)
        {
            var par = new List<byte>();
            par.Add((byte)journalNumber);
            par.Add((byte)eventNumber);
            return CreateMessage(address, selfAddress, CreatePal(0x0138, par, password));
        }
        #endregion


        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static int GetMonth(DateTime less, DateTime more)
        {
            if (less == more) return 0;

            if (less > more) return -1;

            return (more.Year - less.Year) * 12 + more.Month - less.Month;
        }

        /// <summary>
        /// Проверяет пакет на правильность, убирает экранированные символы, отсекает заголовок и концовку, оставляет только чистые данные
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="errorMessage"> </param>
        /// <returns></returns>
        public static List<byte> CheckPacket(List<byte> bytes, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (bytes == null) return null;

            if (bytes.Count < 11)
            {
                errorMessage = "Короткий пакет";
                return null;
            }

            if (bytes[0] != END)
            {
                errorMessage = "Первый байт пакета - не " + END.ToString("X2");
                return null;
            }
            if (bytes[bytes.Count - 1] != END)
            {
                errorMessage = "Последний байт пакета - не " + END.ToString("X2");
                return null;
            }
            if (bytes[1] != OPT)
            {
                errorMessage = "OPT не совпадает с OPT запроса";
                return null;
            }
            var serv = bytes[6];

            var classAccess = serv & ClassAccessMask;
            if (classAccess != ClassAccess)
            {
                if (classAccess == 0x07)
                {
                    errorMessage = "Нет доступа к данным";
                }
                else
                {
                    errorMessage = "Код ошибки - " + classAccess.ToString("X2");
                }
                return null;
            }

            var dataLength = serv & DataLengthMask;

            var res = new List<byte>(bytes.Skip(1).Take(bytes.Count-2));
            //заменим экранированные символы
            ReplaceEscSymbol(res);

            if (!CheckCrc8(res))
            {
                errorMessage = "Не сошлась контрольная сумма";
                return null;
            }
            res.RemoveRange(0, 8);
            res.RemoveRange(res.Count-1, 1);

            return res;
        }
        private static void ReplaceEscSymbol(List<byte> bytes)
        {
            if(bytes == null)return;

            int index = 0;
            byte symbolToReplace = 0;
            while(true)
            {
                index = bytes.IndexOf(ESC, index);
                if(index==-1 || index == bytes.Count-1) break;

                var secSym = bytes[index + 1];

                if(secSym==ScreeningESC)
                    symbolToReplace = ESC;
                if (secSym == ScreeningEND)
                    symbolToReplace = END;

                bytes.RemoveAt(index + 1);
                bytes.RemoveAt(index);
                bytes.Insert(index, symbolToReplace);
            }
        }
        private static void InsertEscSymbol(List<byte> bytes)
        {
            if (bytes == null) return;

            int index = 0;
            while (true)
            {
                index = bytes.IndexOf(ESC, index);
                if(index == -1) break;

                //bytes.RemoveAt(index);
                bytes.Insert(index+1, ScreeningESC);
            }
            index = 0;
            while (true)
            {
                index = bytes.IndexOf(END, index);
                if (index == -1) break;

                bytes.RemoveAt(index);
                bytes.Insert(index, ESC);
                bytes.Insert(index + 1, ScreeningEND);
            }
        }
        #region Crc8
        private static void ComputeChecksum(IList<byte> bytes)
        {
            byte crc8 = 0;
            for (int i = 0; i < bytes.Count; ++i)
            {
                crc8 = (byte)(CRC8_Table[crc8 ^ bytes[i]]);
            }
            bytes.Add(crc8);
        }
        private static byte GetCheksum(IEnumerable<byte> bytes)
        {
            byte crc8 = 0;
            foreach (var b in bytes)
            {
                crc8 = (byte)(CRC8_Table[crc8 ^ b]);
            }
            return crc8;
        }
        private static bool CheckCrc8(List<byte> message)
        {
            if (message == null || message.Count < 4) return false;
            var l = message.Count;
            var mes = message.Take(l - 1);
            var crc = GetCheksum(mes);
            return crc == message[l - 1];
        }

        static byte[] CRC8_Table = new byte[]
    {
   0x00, 0xb5, 0xdf, 0x6a, 0x0b, 0xbe, 0xd4, 0x61, 0x16, 0xa3, 0xc9, 0x7c, 0x1d, 0xa8, 0xc2, 0x77,
0x2c, 0x99, 0xf3, 0x46, 0x27, 0x92, 0xf8, 0x4d, 0x3a, 0x8f, 0xe5, 0x50, 0x31, 0x84, 0xee, 0x5b, 
0x58, 0xed, 0x87, 0x32, 0x53, 0xe6, 0x8c, 0x39, 0x4e, 0xfb, 0x91, 0x24, 0x45, 0xf0, 0x9a, 0x2f, 
0x74, 0xc1, 0xab, 0x1e, 0x7f, 0xca, 0xa0, 0x15, 0x62, 0xd7, 0xbd, 0x08, 0x69, 0xdc, 0xb6, 0x03,
0xb0, 0x05, 0x6f, 0xda, 0xbb, 0x0e, 0x64, 0xd1, 0xa6, 0x13, 0x79, 0xcc, 0xad, 0x18, 0x72, 0xc7,
0x9c, 0x29, 0x43, 0xf6, 0x97, 0x22, 0x48, 0xfd, 0x8a, 0x3f, 0x55, 0xe0, 0x81, 0x34, 0x5e, 0xeb, 
0xe8, 0x5d, 0x37, 0x82, 0xe3, 0x56, 0x3c, 0x89, 0xfe, 0x4b, 0x21, 0x94, 0xf5, 0x40, 0x2a, 0x9f, 
0xc4, 0x71, 0x1b, 0xae, 0xcf, 0x7a, 0x10, 0xa5, 0xd2, 0x67, 0x0d, 0xb8, 0xd9, 0x6c, 0x06, 0xb3,
0xd5, 0x60, 0x0a, 0xbf, 0xde, 0x6b, 0x01, 0xb4, 0xc3, 0x76, 0x1c, 0xa9, 0xc8, 0x7d, 0x17, 0xa2,
0xf9, 0x4c, 0x26, 0x93, 0xf2, 0x47, 0x2d, 0x98, 0xef, 0x5a, 0x30, 0x85, 0xe4, 0x51, 0x3b, 0x8e, 
0x8d, 0x38, 0x52, 0xe7, 0x86, 0x33, 0x59, 0xec, 0x9b, 0x2e, 0x44, 0xf1, 0x90, 0x25, 0x4f, 0xfa, 
0xa1, 0x14, 0x7e, 0xcb, 0xaa, 0x1f, 0x75, 0xc0, 0xb7, 0x02, 0x68, 0xdd, 0xbc, 0x09, 0x63, 0xd6,
0x65, 0xd0, 0xba, 0x0f, 0x6e, 0xdb, 0xb1, 0x04, 0x73, 0xc6, 0xac, 0x19, 0x78, 0xcd, 0xa7, 0x12,
0x49, 0xfc, 0x96, 0x23, 0x42, 0xf7, 0x9d, 0x28, 0x5f, 0xea, 0x80, 0x35, 0x54, 0xe1, 0x8b, 0x3e, 
0x3d, 0x88, 0xe2, 0x57, 0x36, 0x83, 0xe9, 0x5c, 0x2b, 0x9e, 0xf4, 0x41, 0x20, 0x95, 0xff, 0x4a, 
0x11, 0xa4, 0xce, 0x7b, 0x1a, 0xaf, 0xc5, 0x70, 0x07, 0xb2, 0xd8, 0x6d, 0x0c, 0xb9, 0xd3, 0x66 
};
        #endregion
    }

}


