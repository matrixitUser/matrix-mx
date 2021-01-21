using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
        private dynamic MakeRecord(string type, string parameter, double value, string unit, DateTime date, int reliability = 0, int ns = 0, int weight = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.i1 = reliability;
            record.i2 = weight;
            record.i3 = ns;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeConstRecord(string name, object value, DateTime date, int reliability = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.i1 = reliability;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date, int reliability = 0, int ns = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.i1 = reliability;
            record.i3 = ns;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date, int reliability = 0, int ns = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.i1 = reliability;
            record.i3 = ns;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int reliability = 0, int ns = 0, int weight = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.i1 = reliability;
            record.i2 = weight;
            record.i3 = ns;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date, int reliability = 0, int ns = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.i1 = reliability;
            record.i3 = ns;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
        
        private dynamic MakeResult(int code, DeviceError errorcode, string description)
        {
            dynamic res = new ExpandoObject();

            switch(errorcode)
            {
                case DeviceError.NO_ANSWER:
                    res.code = 310;
                    break;

                default:
                    res.code = code;
                    break;
            }

            res.description = description;
            return res;
        }
    }
}
