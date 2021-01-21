using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetCurrentA(byte na, dynamic passport)
        {
            return ParseCurrentA(GetBlocks(na, 0xc1, 1, 1, 129), passport);
        }

        private dynamic ParseCurrentA(byte[] bytes, dynamic passport)
        {
            dynamic current = new ExpandoObject();
            current.success = true;

            current.records = new List<dynamic>();

            if (bytes == null || bytes.Length < 129)
            {
                current.success = false;
                current.error = "недостаточно данных";
                return current;
            }

            var minutes = BitConverter.ToInt32(bytes, 124);
            current.date = new DateTime(2000, 1, 1).AddSeconds(minutes);

            for (int i = 0; i < 31; i++)
            {
                var channel = passport.channels[i];
                if (channel.isOff())
                {
                    continue;
                }

                var value = BitConverter.ToSingle(bytes, i * 4);
                //current.records.Add(MakeCurrentRecord(string.Format("{0}", channel.name, channel.koefficient), value, channel.unit, current.date));
                current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", channel.name, channel.number), value, channel.unit, current.date));
            }

            return current;
        }

    }
}
