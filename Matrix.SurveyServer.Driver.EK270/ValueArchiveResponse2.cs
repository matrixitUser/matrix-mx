using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.EK270
{
    enum RecordType
    {
        Hour, Day, Uncknown
    }

    class ValueArchiveResponse2
    {
        public IEnumerable<Data> Records { get; private set; }

        public bool IsUncnownRecordType { get; private set; }

        public ValueArchiveResponse2(byte[] data, float version, DevType devType)
        {
            if (data == null || data.Length < 3)
            {
                return;
            }
            //исключаем начальный STX
            var str = Encoding.ASCII.GetString(data, 1, data.Length - 1);

            IsUncnownRecordType = false;

            Driver.Log(string.Format("весь {0}", str));

            char stx = (char)Request.STX;
            var rows = str.Split(stx);
            var records = new List<Data>();
            foreach (var row in rows)
            {
                Driver.Log(string.Format("строка {0}", row));
                try
                {
                    var cells = row.Replace(")(", "\n").Replace("(", "").Replace(")", "").Split('\n');

                    if (devType == DevType.Ek260)
                    {
                        if (version > 2.0)
                        {
                            var evt = cells[cells.Length - 2];

                            var type = RecordType.Hour;
                            switch (evt)
                            {
                                case "0x8103": type = RecordType.Day; break;
                                case "0x8104": type = RecordType.Hour; break;                                
                            }

                            if (type == RecordType.Uncknown)
                            {
                                continue;
                            }

                            var date = ParseDate(cells[2]);
                            switch (type)
                            {
                                case RecordType.Day:
                                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                    break;
                                case RecordType.Hour:
                                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                    break;
                            }
                            int pos = 0;
                            records.Add(new Data(Glossary.GONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.AONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            ++pos;//дата
                            records.Add(new Data(Glossary.Vb, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.VbT, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.V, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.Vo, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.pMP, MeasuringUnitType.Bar, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.TMP, MeasuringUnitType.C, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.KMP, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.CMP, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            if (cells.Length == 20)
                            {
                                records.Add(new Data(Glossary.dpTe, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                                records.Add(new Data(Glossary.T2Tek, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            }
                            records.Add(new Data(Glossary.St2, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.St4, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.St7, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.St6, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.StSy, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        }
                        else if (version < 1.11)
                        {
                            Driver.Log(string.Format("версия {0}", version));
                        }
                        else if (version > 20)
                        {
                            Driver.Log(string.Format("версия {0}", version));
                        }
                    }
                    else if (devType == DevType.Ek270)
                    {
                        //(18484)(640)(2014-05-02,10:00:00)(882066.6309)(884266.5191)(67859)(68044)(12.9437)(10.16)(0.97417)(13.57199)(7)(14)(0)(0)(0)(0x8103)(CRC Ok)
                        var evt = cells[cells.Length - 2];

                        var type = RecordType.Uncknown;
                        Driver.Log(string.Format("evt = {0}", evt));
                        switch (evt)
                        {
                            case "0x8103": type = RecordType.Day; break;
                            case "0x8104": type = RecordType.Hour; break;
                        }

                        if (type == RecordType.Uncknown)
                        {
                            Driver.Log(string.Format("мимо"));
                            continue;
                        }

                        var date = ParseDate(cells[2]);
                        switch (type)
                        {
                            case RecordType.Day:
                                date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                break;
                            case RecordType.Hour:
                                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                break;
                        }
                        //Driver.Log(string.Format("дата {0:dd.MM.yy HH:mm:ss}", date));
                        int pos = -1;
                        records.Add(new Data(Glossary.GONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.AONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        ++pos;//дата
                        records.Add(new Data(Glossary.Vb, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.VbT, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.V, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.Vo, MeasuringUnitType.m3, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.pMP, MeasuringUnitType.Bar, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.TMP, MeasuringUnitType.C, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.KMP, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.CMP, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        if (cells.Length >= 20)
                        {
                            records.Add(new Data(Glossary.dpTe, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                            records.Add(new Data(Glossary.T2Tek, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        }
                        records.Add(new Data(Glossary.St2, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.St4, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.St7, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.St6, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        records.Add(new Data(Glossary.StSy, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                    }
                    else if (devType == DevType.Tc210 || devType == DevType.Tc215)
                    {
                        //(13484)(2014-05-28,11:00:00)(4945.2514)(4945.2514)(4955.0000)(4955.0000)(28.16)(104.3250)(1.0017)(0)(0)(0)(0x8104)(CRC Ok)
                        var evt = cells[cells.Length - 2];

                        var type = RecordType.Uncknown;
                        switch (evt)
                        {
                            case "0x8103": type = RecordType.Day; break;
                            case "0x8104": type = RecordType.Hour; break;
                        }

                        if (type == RecordType.Uncknown)
                        {
                            continue;
                        }

                        var date = ParseDate(cells[1]);
                        switch (type)
                        {
                            case RecordType.Day:
                                date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                break;
                            case RecordType.Hour:
                                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                break;
                        }
                        int pos = 0;
                        records.Add(new Data(Glossary.GONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[pos++])));
                        //records.Add(new Data(Glossary.AONo, MeasuringUnitType.Unknown, date, ParseFloat(cells[++pos])));
                        pos++;//дата
                        records.Add(new Data(Glossary.Vb, MeasuringUnitType.m3, date, ParseFloat(cells[pos++])));
                        records.Add(new Data(Glossary.VbT, MeasuringUnitType.m3, date, ParseFloat(cells[pos++])));
                        records.Add(new Data(Glossary.V, MeasuringUnitType.m3, date, ParseFloat(cells[pos++])));
                        records.Add(new Data(Glossary.Vo, MeasuringUnitType.m3, date, ParseFloat(cells[pos++])));

                        records.Add(new Data(Glossary.TMP, MeasuringUnitType.C, date, ParseFloat(cells[pos++])));
                        records.Add(new Data(Glossary.pMP, MeasuringUnitType.Bar, date, ParseFloat(cells[pos++])));

                        records.Add(new Data(Glossary.KMP, MeasuringUnitType.Unknown, date, ParseFloat(cells[pos++])));
                    }
                }
                catch (Exception ex)
                {
                    Driver.Log(string.Format("ошибочка {0}", ex.StackTrace));
                }
            }
            Records = records;
        }

        private double ParseFloat(string stringValue)
        {
            double f = 0f;
            double.TryParse(stringValue.Replace('.', ','), out f);
            Driver.Log(string.Format("будут распарсены {0}", stringValue));
            Driver.Log(string.Format("был распарсен {0}", f));
            return f;
        }

        private DateTime ParseDate(string stringValue)
        {
            return DateTime.ParseExact(stringValue, "yyyy-MM-dd,HH:mm:ss", null);
        }
    }
}
