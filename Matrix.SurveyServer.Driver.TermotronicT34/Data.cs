using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        dynamic MakeDataRequest(ArrayType array, byte month, byte tariff)
        {
            byte x = (byte)((byte)array << 4);
            byte y = (byte)(month & 0xf);
            byte z = (byte)(x + y);

            var Data = new List<byte>();
            Data.Add(z);
            Data.Add(tariff);

            return MakeBaseRequest(0x05, Data);
        }

        dynamic ParseDataResponse(dynamic answer, DateTime date, byte tariff, int A)
        {
            if (!answer.success) return answer;

            var tariffText = tariff == 0 ? "сумма тарифов" : "тариф " + tariff;

            var records = new List<dynamic>();

            /// Если поле данных ответа содержит 16 байт, то отводится по четыре двоичных байта
            /// на каждый вид энергии в последовательности: активная прямая (А+), активная обратная
            /// (А-), реактивная прямая (R+), реактивная обратная (R-).

            var data = answer.Body;

            //log("ParseDataResponse data length=" + data.Length);

            if (data.Length == 16)
            {
                var offset = 0;
                var ap = (double)Helper.ToInt32(data, offset + 0) / (2.0 * A);
                var am = (double)Helper.ToInt32(data, offset + 4) / (2.0 * A);
                var pp = (double)Helper.ToInt32(data, offset + 8) / (2.0 * A);
                var pm = (double)Helper.ToInt32(data, offset + 12) / (2.0 * A);

                records.Add(MakeDayRecord(string.Format("A+ ({0})", tariffText), ap, "Вт*ч", date));
                records.Add(MakeDayRecord(string.Format("A- ({0})", tariffText), am, "Вт*ч", date));
                records.Add(MakeDayRecord(string.Format("R+ ({0})", tariffText), pp, "Вт*ч", date));
                records.Add(MakeDayRecord(string.Format("R- ({0})", tariffText), pm, "Вт*ч", date));
            }

            /// Если поле данных ответа содержит 12 байт, то отводится по четыре двоичных байта
            /// на каждую фазу энергии А+ в последовательности: активная прямая по 1 фазе, активная
            /// прямая по 2 фазе, активная прямая по 3 фазе.
            if (data.Length == 12)
            {
                var offset = 0;
                var ap1 = Helper.ToInt32(data, offset + 0);
                var ap2 = Helper.ToInt32(data, offset + 4);
                var ap3 = Helper.ToInt32(data, offset + 8);

                records.Add(MakeDayRecord(string.Format("A+ (фаза 1) ({0})", tariffText), ap1, "Вт*ч", date));
                records.Add(MakeDayRecord(string.Format("A+ (фаза 2) ({0})", tariffText), ap2, "Вт*ч", date));
                records.Add(MakeDayRecord(string.Format("A+ (фаза 3) ({0})", tariffText), ap3, "Вт*ч", date));
            }

            answer.records = records;
            return answer;
        }
    }

    enum ArrayType : byte
    {
        EnergyAfterReset = 0x00,
        EnergyOfCurrentYear = 0x01,
        EnergyOfPreviousYear = 0x02,
        EnergyOfMonth = 0x03,
        EnergyOfCurrentDay = 0x04,
        EnergyOfPreviousDay = 0x05,
        ActivePlus = 0x06,
        EnergyOnYearStart = 0x09,
        EnergyOnMonthStart = 0x0b,
        EnergyOnDayStart = 0x0c,
        EnergyOnYesterdayStart = 0x0d
    }
}
