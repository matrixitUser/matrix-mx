using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public enum CurrentParameterType
    {
        Float,
        Date,
        Time
    }

    public struct CurrentParameter
    {
        public byte P;
        public string name;
        public string description;
        public string unit;
        public VersionMask vmask;
        public CurrentParameterType type;
    }
}
