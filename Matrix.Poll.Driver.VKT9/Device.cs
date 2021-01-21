using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.VKT9
{
    internal static class Device
    {
        public enum Error
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION,
            ACCESS_DENIED
        };

        public enum ExtraChType
        {
            NONE = 0,
            WaterGas,
            Electricity
        }


        public static byte[] Send(byte[] data, Func<byte[]> response, Action<byte[]> request, Action<string, int> log, int timeout = 15000)
        {
            var buffer = new List<byte>();

            log(string.Format("-({1})-> {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), 3);

            response();
            request(data);
            
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
            {
                Thread.Sleep(sleep);

                var buf = response();
                if (buf.Any())
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0;
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == 6)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("<-({1})- {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), 3);

            return buffer.ToArray();
        }
        
        public static dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeResult(int code, Device.Error errorcode, string description)
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case Device.Error.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        
        public static bool HasProperty(dynamic dyn, string prop)
        {
            return (dyn is IDictionary<string, object>) && (dyn as IDictionary<string, object>).ContainsKey(prop);
        }

        public static byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        public static byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        public static Int32 ToInt32(byte[] data, int offset)
        {
            var x = data.Skip(offset).Take(4).Reverse().ToArray();
            return BitConverter.ToInt32(x, 0);
        }

        public static Int16 ToInt16(byte[] data, int offset)
        {
            var x = data.Skip(offset).Take(2).Reverse().ToArray();
            return BitConverter.ToInt16(x, 0);
        }

        public static UInt32 ToUInt32(byte[] data, int offset)
        {
            var x = data.Skip(offset).Take(4).Reverse().ToArray();
            return BitConverter.ToUInt32(x, 0);
        }

        public static UInt16 ToUInt16(byte[] data, int offset)
        {
            var x = data.Skip(offset).Take(2).Reverse().ToArray();
            return BitConverter.ToUInt16(x, 0);
        }

        public static float ToSingle(byte[] data, int offset)
        {
            var x = data.Skip(offset).Take(4).Reverse().ToArray();
            return BitConverter.ToSingle(x, 0);
        }

        public static byte ToByte(byte[] data, int offset)
        {
            return data[offset + 1];
        }

        public static double ToLongAndFloat(byte[] data, int offset)
        {
            double result = 0.0;
            result += ToInt32(data, offset);
            result += ToSingle(data, offset + 4);
            return result;
        }
    }
}
