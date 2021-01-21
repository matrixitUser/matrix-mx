using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private dynamic GetHour(byte na, int number)
        {
            dynamic hours = new ExpandoObject();
            hours.success = true;
            hours.records = new List<dynamic>();

            //switch (channel)
            //        {
            //            case 1: return Glossary.Qwt;
            //            case 2: return Glossary.Qnt;
            //            case 4: return Glossary.P;
            //            case 5: return Glossary.T;
            //        }
            //        break;
            //    case 1:
            //        switch (channel)
            //        {
            //            case 1: return Glossary.Qwtns;
            //            case 2: return Glossary.Qntns;
            //        }


            //берем самую новую запись, ежели она больше начальной, идем дальше            
            var h = ParseRecord(Send(MakeRecordRequest(na, 0, 1, number)));
            if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qwt, (double)h.value, GetUnit(Glossary.Qwt), h.date.AddHours(-1)));
            hours.date = h.date.AddHours(-1);
            hours.number = h.number;

            h = ParseRecord(Send(MakeRecordRequest(na, 0, 2, number)));if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qnt, (double)h.value, GetUnit(Glossary.Qnt), h.date.AddHours(-1)));

            // h = ParseRecord(Send(MakeRecordRequest(na, 0, 3, number)));if (!h.success) return h;
            //hours.records.Add(MakeHourRecord(h.parameter, h.value, GetUnit(h.parameter), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 0, 4, number)));if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.P, (double)h.value, GetUnit(Glossary.P), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 0, 5, number)));if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.T, (double)h.value, GetUnit(Glossary.T), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 1, 1, number)));if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qwtns, (double)h.value, GetUnit(Glossary.Qwtns), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 1, 2, number)));if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qntns, (double)h.value, GetUnit(Glossary.Qntns), h.date.AddHours(-1)));

            return hours;
        }

        private dynamic GetHourAbnormal(byte na, int number)
        {
            dynamic hours = new ExpandoObject();
            hours.success = true;
            hours.records = new List<dynamic>();

            //берем самую новую запись, ежели она больше начальной, идем дальше            
            var h = ParseRecord(Send(MakeRecordRequest(na, 1, 1, number)));
            if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qwtns, (double)h.value, GetUnit(Glossary.Qwtns), h.date.AddHours(-1)));
            hours.date = h.date.AddHours(-1);
            hours.number = h.number;

            h = ParseRecord(Send(MakeRecordRequest(na, 1, 2, number)));
            if (!h.success) return h;
            hours.records.Add(MakeHourRecord(Glossary.Qntns, (double)h.value, GetUnit(Glossary.Qntns), h.date.AddHours(-1)));

            return hours;
        }

        private byte[] MakeRecordRequest(byte na, byte group, byte channel, int number)
        {
            var body = new List<byte>();

            body.Add(group);
            body.Add(channel);
            body.AddRange(BitConverter.GetBytes(number).Reverse());

            return MakeModbusRequest(na, 65, body.ToArray());
        }

        private dynamic ParseRecord(byte[] bytes)
        {
            dynamic record = new ExpandoObject();
            record.success = true;

            //check here
            if (bytes.Length < 21)
            {
                record.error = "неполный пакет";
                record.success = false;
                return record;
            }

            record.group = bytes[2];
            record.channel = bytes[3];
            record.number = ToInt32(bytes, 4);

            char recordType = (char)bytes[8];
            var state = bytes[9];          /* состояние 0....4 */
            var year = bytes[10];          /* Временная отметка в качестве местного времени */
            var month = bytes[11];
            var day = bytes[12];
            var hour1 = bytes[13];
            var minute = bytes[14];
            var second = bytes[15];

            record.date = new DateTime(2000 + year, month, day, hour1, minute, second);

            record.value = 0.0;
            switch (recordType)
            {
                case 'F':
                    record.value = ToSingle(bytes, 16);
                    break;
                case 'L':
                    record.value = ToUInt32(bytes, 16);
                    break;
                case 'T':
                    break;
                case 'Z':
                    record.value = ToInt32(bytes, 16) * 1000000000 + ToInt32(bytes, 20);
                    //log(string.Format("запись типа Z ({0})+({1})", ToInt32(bytes, 16), ToInt32(bytes, 20)));
                    break;
                default:
                    break;
            }

            //record.parameter = GetParameterName(record.group, record.channel);
            return record;
        }

        private string GetUnit(string parameter)
        {
            switch (parameter)
            {
                case Glossary.Qwt:
                case Glossary.Qnt:
                case Glossary.Qw:
                case Glossary.Qn:
                    return "м³";
                case Glossary.T: return "°C";
                case Glossary.P: return "бар";
            }
            return "";
        }

        private string GetParameterName(byte group, byte channel)
        {
            switch (group)
            {
                case 0:
                    switch (channel)
                    {
                        case 1: return Glossary.Qwt;
                        case 2: return Glossary.Qnt;
                        case 4: return Glossary.P;
                        case 5: return Glossary.T;
                    }
                    break;
                case 1:
                    switch (channel)
                    {
                        case 1: return Glossary.Qwtns;
                        case 2: return Glossary.Qntns;
                    }
                    break;
                case 8:
                    switch (channel)
                    {
                        case 0:
                        case 1:
                            return Glossary.Qnt;
                        case 2:
                        case 3:
                            return Glossary.Qwt;
                        case 4:
                        case 5:
                            return Glossary.Qn;
                        case 6:
                        case 7:
                            return Glossary.Qw;
                        case 9: return Glossary.P;
                        case 11: return Glossary.T;
                        case 12:
                        case 13:
                            return Glossary.Qnns;
                    }
                    break;
            }
            return string.Format("не определен (группа {0}, канал {1})", group, channel);
        }

       
    }
}
