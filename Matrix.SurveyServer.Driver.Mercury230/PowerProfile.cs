using Matrix.SurveyServer.Driver.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        byte[] MakePowerProfileRequest(UInt32 address, byte count, byte profile)
        {
            var Data = new List<byte>();

            byte strange = 1;
            //byte addressBit = isByte17 ? (byte)0 : (byte)1;
            byte energyType = 0;
            byte memory = 3;

            strange = (byte)(((profile & 0x01) << 7) | ((energyType & 0x07) << 4) | (memory & 0xf));

            Data.Add(strange);
            Data.Add(Helper.GetHighByte(address));
            Data.Add(Helper.GetLowByte(address));
            Data.Add(count);

            return MakeBaseRequest(0x06, Data);
        }


        dynamic ParsePowerProfileResponse(dynamic answer, int A)
        {
            if (!answer.success)
            {
                if (answer.errorcode == DeviceError.CHANNEL_CLOSED)
                {
                    TryChannelOpen();
                }
                return answer;
            }

            var records = new List<dynamic>();

            var start = 0;

            byte[] body = answer.Body as byte[];

            answer.IsEmpty = true;
            if ((body != null) && (body.Length >= 15))
            {
                foreach (byte b in body)
                {
                    if (b != 0x00)
                    {
                        answer.IsEmpty = false;
                        break;
                    }
                }
            }


            if (!answer.IsEmpty)
            {
                var state = answer.Body[start + 0];

                var hour = Helper.FromBCD(answer.Body[start + 1]);
                var minute = Helper.FromBCD(answer.Body[start + 2]);
                var day = Helper.FromBCD(answer.Body[start + 3]);
                var month = Helper.FromBCD(answer.Body[start + 4]);
                var year = 2000 + Helper.FromBCD(answer.Body[start + 5]);

                DateTime Date;
                try
                {
                    Date = new DateTime(year, month, day, hour, minute, 0);

                    if (45 < minute && minute <= 59)
                    {
                        Date = Date.AddHours(1);
                        Date = Date.AddMinutes(-Date.Minute);
                    }
                    else if (0 < minute && minute <= 15)
                    {
                        Date = Date.AddMinutes(-Date.Minute);
                    }
                    else if (15 < minute && minute <= 45)
                    {
                        Date = Date.AddMinutes(-Date.Minute);
                        Date = Date.AddMinutes(30);
                    }
                }
                catch (Exception ex)
                {
                    Date = DateTime.MinValue;
                }

                answer.Date = Date;

                var pp = (double)BitConverter.ToInt16(answer.Body, start + 7); pp = pp == -1.0 ? 0.0 : pp;
                var PPlus1 = pp / (double)A;
                var pm = (double)BitConverter.ToInt16(answer.Body, start + 9); pm = pm == -1.0 ? 0.0 : pm;
                var PMinus1 = pm / (double)A;
                var ap = (double)BitConverter.ToInt16(answer.Body, start + 11); ap = ap == -1.0 ? 0.0 : ap;
                var APlus1 = ap / (double)A;
                var am = (double)BitConverter.ToInt16(answer.Body, start + 13); am = am == -1.0 ? 0.0 : am;
                var AMinus1 = am / (double)A;

                //var next = start + 14;
                //var PPlus2 = (double)BitConverter.ToInt16(data, next + 8) / (double)A;
                //var PMinus2 = (double)BitConverter.ToInt16(data, next + 10) / (double)A;
                //var APlus2 = (double)BitConverter.ToInt16(data, next + 12) / (double)A;
                //var AMinus2 = (double)BitConverter.ToInt16(data, next + 14) / (double)A;

                records.Add(MakeHourRecord("A+", APlus1, "кВт", Date));
                records.Add(MakeHourRecord("A-", AMinus1, "кВт", Date));
                records.Add(MakeHourRecord("Q+", PPlus1, "кВт", Date));
                records.Add(MakeHourRecord("Q-", PMinus1, "кВт", Date));
                records.Add(MakeHourRecord("Байт состояния", state, "", Date));

                answer.pp = pp;
                answer.pp1 = PPlus1;
                answer.ap = ap;
                answer.ap1 = APlus1;
                answer.records = records;
            }
            return answer;
        }
    }
}
