using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using System.Globalization;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.EK270
{
    class ValueArchiveResponse : Response
    {
        public IEnumerable<Data> Records { get; private set; }

        public ValueArchiveResponse(byte[] data, float version, IEnumerable<string> parameters)
            : base(data)
        {
            var records = new List<Data>();

            System.IO.File.AppendAllText(@"d:\log.txt", string.Format("version={0}", version));

            if (version > 2.0)
            {
                var size = parameters.Count();
                System.IO.File.AppendAllText(@"d:\log.txt", string.Format("size={0}", size));
                System.IO.File.AppendAllText(@"d:\log.txt", string.Format("values.count={0}", Values.Count()));
                for (int offset = 0; offset < Values.Count() - size; offset += size)
                {
                    System.IO.File.AppendAllText(@"d:\log.txt", string.Format("values.count={0}", Values.Count()));
                    if (Values.ElementAt(18 + offset) == "0x8104") continue;
                    int position = 0;
                    var date = ParseDate(Values.ElementAt(2 + offset));

                    if (date.Minute != 0 || date.Second != 0) continue;
                    records.Add(new Data(Glossary.GONo, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.AONo, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    position++;
                    records.Add(new Data(Glossary.Vb, MeasuringUnitType.m3, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.VbT, MeasuringUnitType.m3, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.V, MeasuringUnitType.m3, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.Vo, MeasuringUnitType.m3, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.pMP, MeasuringUnitType.Bar, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.TMP, MeasuringUnitType.C, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.KMP, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.CMP, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    if (size == 20)
                    {
                        records.Add(new Data(Glossary.dpTe, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                        records.Add(new Data(Glossary.T2Tek, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    }
                    records.Add(new Data(Glossary.St2, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.St4, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.St7, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.St6, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    records.Add(new Data(Glossary.StSy, MeasuringUnitType.Unknown, date, ParseFloat(Values.ElementAt(position++ + offset))));
                    //records.Add(new Data(Glossary.StAe, MeasuringUnitType.m3, date, ParseFloat(Values.ElementAt(16 + offset))));

                }
            }

            Records = records;
        }

        private float ParseFloat(string stringValue)
        {
            return float.Parse(stringValue, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        private DateTime ParseDate(string stringValue)
        {
            return DateTime.ParseExact(stringValue, "yyyy-MM-dd,HH:mm:ss", null);
        }
    }
}
