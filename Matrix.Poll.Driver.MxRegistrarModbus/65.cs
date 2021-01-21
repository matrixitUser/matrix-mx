using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        enum ArchiveType : short
        {
            Hourly = 0x0000, //архив каналов с предопределенными номерами
            /*Daily = 0x0001,
            Monthly = 0x0002,*/
            Abnormal = 0x0009
        }

        dynamic Make65Request(ArchiveType archiveType, DateTime date)
        {
            var Data = new List<byte>();

            Data.Add(Helper.GetHighByte((UInt16)archiveType));
            Data.Add(Helper.GetLowByte((UInt16)archiveType));

            //количество записей (всегда 1)
            Data.Add(0x00);
            Data.Add(0x01);

            //тип запроса (0x01 - по дате/времени)
            Data.Add(0x01);

            Data.Add((byte)date.Second);
            Data.Add((byte)date.Minute);
            Data.Add((byte)date.Hour);
            Data.Add((byte)date.Day);
            Data.Add((byte)date.Month);
            Data.Add((byte)(date.Year - 2000));

            return MakeBaseRequest(65, Data);
        }

        dynamic Make65Request(ArchiveType archiveType, UInt16 index, bool isValues = false)
        {
            var Data = new List<byte>();

            Data.Add(Helper.GetHighByte((UInt16)archiveType));
            Data.Add(Helper.GetLowByte((UInt16)archiveType));

            //количество записей (всегда 1)
            Data.Add(0x00);
            Data.Add(0x01);

            //тип запроса (0x00 - по индексу)
            Data.Add(isValues ? (byte)0x02 : (byte)0x00);

            Data.Add(Helper.GetHighByte(index));
            Data.Add(Helper.GetLowByte(index));

            return MakeBaseRequest(65, Data);
        }


        public dynamic Parse65Response(dynamic answer, UInt16 devid, Dictionary<int, Parameter> parameterConfiguration)
        {
            if (!answer.success) return answer;

            //List<string> channels = passport.Channels;
            //List<string> units = passport.Units;
            //List<float> muls = passport.Mul;
            //List<float> offs = passport.Offset;

            dynamic ret = new ExpandoObject();
            ret.success = false;
            ret.error = string.Empty;
            ret.errorcode = DeviceError.NO_ERROR;

            ret.Date = DateTime.MinValue;
            ret.Data = new List<dynamic>();

            if (answer.Function != 0x41)
            {
                if (answer.Function == 0xc1)
                {
                    ret.errorcode = DeviceError.DEVICE_EXCEPTION;
                    switch ((byte)answer.Body[0])
                    {
                        case 33:// MODBUS_ERROR_ARCHIVE_NOT_EXIST
                            ret.error = "Архива не существует";
                            break;
                        case 34:// MODBUS_ERROR_ARCHIVE_OUT_OF_RANGE
                            ret.errorcode = DeviceError.NO_ERROR;
                            ret.success = true;
                            break;
                        case 35:// MODBUS_ERROR_ARCHIVE_NULL_RECORD
                            ret.errorcode = DeviceError.NO_ERROR;
                            ret.success = true;
                            break;
                    }
                }
                else
                {
                    ret.errorcode = DeviceError.UNEXPECTED_RESPONSE;
                    ret.error = "Неожиданный ответ на запрос записи архива";
                }
                return ret;
            }

            ret.errorcode = DeviceError.NO_ERROR;
            ret.success = true;

            var length = answer.Body[0];
            const int offset = 1;

            if (GetRegisterSet(devid).name == "new" && GetRegisterSet(devid).Timestamp == null)
            {
                ret.Date = new DateTime(2000 + answer.Body[offset + 0], answer.Body[offset + 1], answer.Body[offset + 2], answer.Body[offset + 3], 0, 0);
            }
            else
            {
                var seconds = Helper.ToUInt32(answer.Body, offset + 0);
                ret.Date = seconds > 0 ? new DateTime(1970, 1, 1).AddSeconds(seconds) : DateTime.MinValue;
            }

            byte channel;
            int i;

            for (i = offset + 4, channel = 0; i < answer.Body.Length - 3; i += 4, channel++)
            {
                if (ret.Date != DateTime.MinValue)
                {
                    string regName = string.Format("Канал {0}", channel + 1);
                    string type;
                    double mul;
                    double off;

                    if (parameterConfiguration.ContainsKey(channel + 1))
                    {
                        var pcfg = parameterConfiguration[channel + 1];
                        regName = pcfg.name == "" ? regName : pcfg.name;
                        mul = pcfg.k;
                        off = pcfg.start;
                        type = pcfg.unit;
                    }
                    else
                    {
                        type = "";
                        mul = 1.0;
                        off = 0.0;
                    }

                    dynamic data = new ExpandoObject();
                    data.name = regName;
                    data.unit = type;
                    data.value = Helper.ToUInt32(answer.Body, i) * mul + off;
                    data.date = ret.Date;
                    ret.Data.Add(data);
                }
            }
            /*
            {
                var dval = (ret.Data[0].value > ret.Data[1].value)? 
                    ret.Data[0].value - ret.Data[1].value : 
                    ret.Data[1].value - ret.Data[0].value;

                dynamic data = new ExpandoObject();
                data.name = "dКанал1,2";
                data.unit = "";
                data.value = dval;
                data.date = ret.Date;
                ret.Data.Add(data);
            }*/

            return ret;
        }



        public dynamic Parse65AbnormalResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            dynamic ret = new ExpandoObject();
            ret.success = false;
            ret.error = string.Empty;
            ret.errorcode = DeviceError.NO_ERROR;

            ret.Date = DateTime.MinValue;
            ret.Data = new List<dynamic>();

            if (answer.Function == 0xc1)
            {
                //no record
            }

            if (answer.Function != 0x41)
            {
                ret.error = "Неожиданный ответ на запрос записи архива";
                ret.errorcode = DeviceError.UNEXPECTED_RESPONSE;
                return ret;
            }

            ret.errorcode = DeviceError.NO_ERROR;
            ret.success = true;

            var length = answer.Body[0];
            const int offset = 1;

            var seconds = Helper.ToUInt32(answer.Body, offset + 0);
            ret.Date = seconds > 0 ? new DateTime(1970, 1, 1).AddSeconds(seconds) : DateTime.MinValue;

            int i = offset + 4;

            ret.eventId = -1;
            ret.eventDescription = "Нет события";

            if (ret.Date != DateTime.MinValue)
            {
                ret.eventId = Helper.ToUInt32(answer.Body, i);
                ret.eventDescription = Abnormal_GetDescriptionById(ret.eventId);
            }

            return ret;
        }





        public dynamic Parse65ValuesResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            int cnt = 16;
            byte[] body = (answer.Body as byte[]).Skip(1).ToArray();

            ArchiveRecord rec = new ArchiveRecord();
            rec.Ts = Helper.ToUInt32(body, 0);
            rec.Counter = new ArchiveCounter[cnt];
            
            for (int i = 0; i < cnt; i++)
            {
                int offset = 4 + i * 8;
                UInt32 count = Helper.ToUInt32(body, offset);
                rec.Counter[i].Value = count & 0x00FFFFFF;
                rec.Counter[i].IsEnabled = (count & 0x80000000) > 0;
                rec.Counter[i].IsError = (count & 0x40000000) > 0;
                rec.Counter[i].PinMagState = (count & 0x20000000) > 0;
                rec.Counter[i].PinState = (count & 0x10000000) > 0;
                UInt16 par = Helper.ToUInt16(body, offset + 4);
                rec.Counter[i].Param = GetParameterName4(par, i + 1);
                rec.Counter[i].Point = (SByte)body[offset + 6];
                rec.Counter[i].Unit = body[offset + 7];
            }
            
            //

            dynamic ret = new ExpandoObject();
            ret.success = true;
            ret.error = string.Empty;
            ret.errorcode = DeviceError.NO_ERROR;
            ret.Date = (rec.Ts == 0xFFFFFFFF || rec.Ts == 0x00000000) ? DateTime.MinValue : new DateTime(1970, 1, 1).AddSeconds(rec.Ts);
            ret.Record = rec;
            return ret;
        }
    }

}
