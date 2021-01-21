using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TEM104
{
    public class RawData
    {
        public double[] Value { get; set; }
        public MeasuringUnitType MeasuringUnit { get; private set; }
        public string Parameter { get; private set; }

        public RawData(string p, int vs, MeasuringUnitType m)
        {
            Value = new double[vs];
            MeasuringUnit = m;
            Parameter = p;
        }
    }
}
