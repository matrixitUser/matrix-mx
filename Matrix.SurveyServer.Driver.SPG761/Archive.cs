using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        public const string DAY = "65532";
        public const string HOUR = "65530";

        private dynamic GetHeadArchive(byte dad, byte sad, bool needDad, string type)
        {
            List<byte> dataSet = new List<byte>();

            dataSet.Add(HT);
            dataSet.AddRange(Encode("0"));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(type));
            dataSet.Add(FF);

            var bytes = MakeBaseRequest(dad, sad, 0x19, Stuffing(dataSet), needDad);
            dynamic answer = ParseArrayResponse(SendWithCrc(bytes.ToArray()));

            if (!answer.success)
                return answer;

            dynamic head = new ExpandoObject();
            head.success = true;
            head.error = string.Empty;

            var parameters = new List<dynamic>();

            foreach (var category in answer.categories)
            {
                if (category.Length < 4)
                {
                    break;
                }

                var last = parameters.LastOrDefault();
                dynamic parameter = new ExpandoObject();

                parameter.name = category[0];
                parameter.unit = category[1];
                foreach (var key in replaces.Keys)
                {
                    parameter.unit = parameter.unit.Replace(key, replaces[key]);
                }


                if (string.IsNullOrWhiteSpace(category[0]) && last != null)
                {
                    parameter.name = last.name;
                }

                if (string.IsNullOrEmpty(category[1]) && last != null)
                {
                    parameter.unit = last.unit;
                }

                parameter.channel = category[2];
                parameter.code = category[3];
                parameters.Add(parameter);
                // log(string.Format("параметр {0};{1};{2};{3}", parameter.name, parameter.unit, parameter.channel, parameter.code));
            }
            head.parameters = parameters;
            return head;
        }

        //private dynamic GetArchive(byte na, byte pass, DateTime date, string type, dynamic parameters, int version)
        //{
        //    dynamic archive = new ExpandoObject();
        //    archive.date = date;
        //    archive.success = true;
        //    archive.error = string.Empty;

        //    if (type == HOUR)
        //        date = date.AddHours(1);
        //    if (type == DAY)
        //        date = date.AddDays(2);

        //    var bytes = MakeArchiveRequest(na, pass, date, type);
        //    dynamic answer = ParseArrayResponse(SendWithCrc(bytes));
        //    if (!answer.success)
        //        return answer;

        //    if (parameters == null || parameters.Count == 0)
        //    {
        //        answer.success = false;
        //        answer.error = "параметры не заданы";
        //        return answer;
        //    }

        //    List<dynamic> records = new List<dynamic>();

        //    var index = -1;
        //    Char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

        //    var categories = (answer.categories as IEnumerable<string[]>);
        //    foreach (var category in categories)
        //    {
        //        // сведения о датах, в разных версиях разное количество
        //        if (category.Length == 6) continue;
        //        index++;
        //        double value = 0.0;
        //        if (!double.TryParse(category[0].Replace('.', separator), out value))
        //        {
        //            continue;
        //        }
        //        records.Add(MakeArchiveRecord(GetParameter(parameters[index].name), value, parameters[index].unit, archive.date, type));
        //    }
        //    archive.records = records;
        //    return archive;
        //}

        //private byte[] MakeArchiveRequest(byte dad, byte sad, DateTime date, string param)
        //{
        //    var bytes = new List<byte>();
        //    bytes.AddRange(MakeHeader(dad, sad, 0x18));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode("0"));
        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(param));
        //    bytes.Add(FF);

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Day.ToString()));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Month.ToString()));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Year.ToString()));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Hour.ToString()));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Minute.ToString()));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(date.Second.ToString()));
        //    bytes.Add(FF);

        //    bytes.Add(DLE);
        //    bytes.Add(ETX);

        //    var crc = CrcCalc(bytes.ToArray(), 2, bytes.Count - 2);
        //    bytes.AddRange(crc);
        //    return bytes.ToArray();
        //}

        private dynamic GetArchive(byte dad, byte sad, bool needDad, DateTime date, string type, byte version, dynamic head)
        {
            dynamic archive = new ExpandoObject();
            archive.date = date;
            archive.success = true;
            archive.error = string.Empty;

            if (type == HOUR)
                date = date.AddHours(1);
            if (type == DAY)
            {
                if (version == 0)
                    date = date.AddDays(2);
                else
                    date = date.AddDays(1);
            }
            dynamic answer = GetArhive(dad, sad, needDad, "00", type, date);
            if (!answer.success)
                return answer;

            if (head == null || head.Count == 0)
            {
                answer.success = false;
                answer.error = "параметры не заданы";
                return answer;
            }

            List<dynamic> records = new List<dynamic>();

            var index = -1;
            Char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

            var categories = (answer.categories as IEnumerable<string[]>);
            foreach (var category in categories)
            {
                // сведения о датах, в разных версиях разное количество
                if (category.Length == 6) continue;
                index++;
                double value = 0.0;
                if (!double.TryParse(category[0].Replace('.', separator), out value))
                {
                    continue;
                }
                records.Add(MakeArchiveRecord(GetParameter(head[index].name), value, head[index].unit, archive.date, type));
            }
            archive.records = records;
            return archive;
        }

        private dynamic MakeArchiveRecord(string parameter, double value, string unit, DateTime date, string type)
        {
            dynamic record = new ExpandoObject();

            if (type == DAY)
            {
                record.type = "Day";
                date = date.Date;
            }
            else
                record.type = "Hour";

            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private string GetParameter(string content)
        {
            return content.Replace("т0", "т").Replace("(с)", "").Replace("(ч)", "");
        }
    }
}
