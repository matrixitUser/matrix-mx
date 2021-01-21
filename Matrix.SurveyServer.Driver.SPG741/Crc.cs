using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        /// <summary>
        /// Контрольная сумма представляет собой побитно инвертированный
        /// младший байт суммы всех предшествующих байтов за исключением
        /// кода начала кадра (байты 2...7).
        /// </summary>
        /// <param name="bytes">данные</param>
        /// <param name="offset">смещение</param>
        /// <param name="length">число байтов для расчета</param>
        /// <returns></returns>
        private byte CalcCrc(byte[] bytes, int offset, int length)
        {
            byte CS = 0;
            for (int i = offset; i < (length + offset); i++) { CS += bytes[i]; }
            return (byte)(CS ^ 0xFF);
        }

        private bool CheckCrc(byte[] bytes)
        {
            if (bytes == null || (bytes.Length < 6)) return false;

            var crcClc = CalcCrc(bytes, 1, bytes.Length - 3);
            var crcMsg = bytes[bytes.Length - 2];
            
            return crcClc == crcMsg;
        }
    }
}
