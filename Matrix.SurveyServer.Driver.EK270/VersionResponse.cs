using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    class VersionResponse
    {
        public float Version { get; private set; }

        public VersionResponse(byte[] data)
        {
            Version = 0f;
            var str = Encoding.GetEncoding(1252).GetString(data);
            Driver.Log(str);
            var rgx = new Regex(@"\((?<version>\d+(\.\d+)?)\)");
            var match = rgx.Match(str);
            if (match != null)
            {
                var strVersion = match.Groups["version"].Value;
                Driver.Log(strVersion);
                float v = 0f;
                float.TryParse(strVersion.Replace('.', ','), out v);
                Version = v;
            }            
        }
    }
}
