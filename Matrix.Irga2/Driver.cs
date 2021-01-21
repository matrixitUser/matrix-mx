using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.Irga2
{
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var parameters = (IDictionary<string, object>)arg;

            if (parameters.ContainsKey("start") && arg.start is DateTime)
            {
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }

            if (parameters.ContainsKey("end") && arg.end is DateTime)
            {
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }

            var components = "Hour;Day;Constant;Abnormal;Current";
            if (parameters.ContainsKey("components"))
            {
                components = arg.components;
                log(string.Format("указаны архивы {0}", components));
            }
            else
                log(string.Format("архивы не указаны, будут опрошены все"));

            switch (what.ToLower())
            {
                case "all": return All(components);
            }

            log(string.Format("неопознаная команда {0}", what));
            return MakeResult(201, what);
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        dynamic All(string components)
        {
            try
            {


                ////состав
                //SendWithCrc(new byte[] { 0x6d });

                //var ans1 = SendWithCrc(new byte[] { (byte)'R' });
                //if(ans1.Length==1)
                //{
                //    ans1 = SendWithCrc(new byte[] { });
                //}else
                //{
                //    ans1 = ans1.Skip(1).ToArray();
                //}
                //var date = new DateTime(
                //        BinDecToInt(ans1[22]) + 2000,
                //        BinDecToInt(ans1[21]),
                //        BinDecToInt(ans1[20]),
                //        BinDecToInt(ans1[19]),
                //        BinDecToInt(ans1[18]),
                //        0
                //    );

                //log(string.Format("дата {0:dd.MM.yyyy HH:mm:ss}", date));

                //var constants = new List<dynamic>();
                //constants.Add(MakeConstantRecord("Барометрическое давление, кг/см²", BitConverter.ToSingle(ans1, 1), date));
                //constants.Add(MakeConstantRecord("Плотность газа при станд. усл., кг/м³", BitConverter.ToSingle(ans1, 6), date));
                //constants.Add(MakeConstantRecord("CO₂", BitConverter.ToSingle(ans1, 10), date));
                //constants.Add(MakeConstantRecord("N₂", BitConverter.ToSingle(ans1, 14), date));
                //records(constants);

                //////
                //var ans = SendWithCrc(new byte[] { 0x6e });

                //var size = BitConverter.ToInt16(ans, 1);
                //var id = (char)ans[3];
                //var ch = ans[4];
                //var ns = (char)ans[5];
                //var fl = ans[6];
                //var p = BitConverter.ToSingle(ans, 7);
                //var t = BitConverter.ToSingle(ans, 11);
                //var q1 = BitConverter.ToSingle(ans, 15);
                //var q2 = BitConverter.ToSingle(ans, 19);
                //var q3 = BitConverter.ToSingle(ans, 23);
                //var q4 = BitConverter.ToSingle(ans, 27);
                //var q5 = BitConverter.ToSingle(ans, 31);

                //log(string.Format("size={0}; id={1}; ch={2}; ns={3}; fl={4}; p={5}; t={6}; {7} {8} {9} {10} {11}", size, id, ch, ns, fl, p, t, q1, q2, q3, q4, q5));

                log("-> 'SYS'");
                var foo = Send(new byte[] { (byte)'S', (byte)'Y', (byte)'S' });
                var irga = Encoding.GetEncoding(866).GetString(foo.Skip(1).ToArray());
                log(irga);

                log("-> N (=0)");
                request(new byte[] { 10 });
                //var ha3 = SendWithCrc(new byte[] { 0 });

                //var x = SendWithCrc(GetMemoryRequest('F',0,2,5));
                //var y = SendWithCrc(GetMemoryRequest('F', 0x00f0, 0, 0x10));

                if (components.Contains("Hour"))
                {
                    SendWithCrc(GetCalendarRequest(0x00ff,0));
                    
                    var now = DateTime.Now;
                    var monthStart = new DateTime(now.Year, now.Month, 1);
                    var hour = (int)(now - monthStart).TotalHours-3;
                    log("запрос часа "+hour+" от начала месяца");
                    //var ans = SendWithCrc(GetMemoryRequest('F', (short)(26*hour), 1, 26));

                }
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка {0} {1}", ex.Message, ex.StackTrace));
            }
            return MakeResult(0);
        }

        private byte[] GetCalendarRequest(short addr, byte n)
        {
            var bts = new List<byte>();
            bts.Add(0xfa);

            bts.AddRange(MakeRequest(new byte[] {
                1,
                (byte)'R',
                GetHighByte(addr),
                GetLowByte(addr),                
                //n
            }));
            return bts.ToArray();
        }

        private byte[] GetMemoryRequest(short addr, byte sector, byte n)
        {
            return MakeRequest(new byte[] {
                1,
                (byte)'F',
                GetLowByte(addr),
                GetHighByte(addr),
                sector,
                n
            });
        }

        private byte[] MakeRequest(byte[] raw)
        {
            var res = new byte[raw.Length * 2];

            for (int i = 0, j = 0; j < raw.Length; i += 2, j++)
            {
                res[i] = (byte)(~raw[j]);
                res[i + 1] = raw[j];
            }
            return res;
        }

        public static byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        public static byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        private int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }
    }
}

