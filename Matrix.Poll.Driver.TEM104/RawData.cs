using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.TEM104
{
    public class RawData
    {
        public double[] Value { get; set; }
        public string MeasuringUnit { get; private set; }
        public string Parameter { get; private set; }

        public RawData(string p, int vs, string m)
        {
            Value = new double[vs];
            MeasuringUnit = m;
            Parameter = p;
        }
    }
}
