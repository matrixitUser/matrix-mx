using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    class SingleValueResponse
    {
        private readonly static Regex regex = new Regex(@"\((?<foo>\d+(\.\d+)?)\*");

        public double Value { get; private set; }

        public SingleValueResponse(byte[] data)
        {
            var str = Encoding.ASCII.GetString(data);
            var match = regex.Match(str);
            if (match.Success)
            {
                var strValue = match.Groups["foo"].Value.Replace('.', ',');
                double val = 0;
                double.TryParse(strValue, out val);
                Value = val;
            }
        }
    }
}
