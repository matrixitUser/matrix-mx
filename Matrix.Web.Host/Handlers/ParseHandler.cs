using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Matrix.Web.Host.Data;
using CoordinateSharp;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Handlers
{
    class ParseHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("parse");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            var userId = Guid.Parse(session.userId.ToString());
            
            if (what == "parse-matrix-terminal-config-from-string")
            {
                var answer = Helper.BuildMessage(what);

                var dicBody = message.body as IDictionary<string, object>;
                if (dicBody.ContainsKey("strConfig"))
                {
                    List<dynamic> listProfile = new List<dynamic>();
                    List<string> listApnName = new List<string>();
                    string strConfig = message.body.strConfig.ToString();
                    var bytesConfig = strConfig.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    MatrixTerminalConfig conf = StructsHelper.Instance.setBytesFromConfig<MatrixTerminalConfig>(bytesConfig, new MatrixTerminalConfig());
                    answer.body.config = conf;

                    foreach(var profile in conf.profile)
                    {
                        dynamic profileTmp = new ExpandoObject();
                        string ipPort = StructsHelper.Instance.ParseStringFromBytes(profile.ip_port);

                        profileTmp.ip = (ipPort.Contains(':'))? ipPort.Split(':')[0]: ipPort;
                        profileTmp.port = (ipPort.Contains(':')) ? ipPort.Split(':')[1] : "";
                        listProfile.Add(profileTmp);
                    }
                    foreach (var apnName in conf.apnName)
                    {
                        listApnName.Add(StructsHelper.Instance.ParseStringFromBytes(apnName.APN));
                    }
                    answer.body.profiles = listProfile;
                    answer.body.APNs = listApnName;
                }
                
                return answer;
            }

            if (what == "parse-matrix-terminal-get-string-from-config")
            {
                var answer = Helper.BuildMessage(what);
                var config = message.body.config;
                var profiles = message.body.profiles;
                var APNs = message.body.APNs;
                var dicBody = message.body as IDictionary<string, object>;
                if (dicBody.ContainsKey("strConfig"))
                {
                    List<dynamic> listProfile = new List<dynamic>();
                    List<string> listApnName = new List<string>();
                    string strConfig = message.body.strConfig.ToString();
                    var bytesConfig = strConfig.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    MatrixTerminalConfig conf = StructsHelper.Instance.setBytesFromConfig<MatrixTerminalConfig>(bytesConfig, new MatrixTerminalConfig());
                    for(int i = 0; i < conf.profile.Length; i++)
                    {
                        conf.profile[i].ip_port = StructsHelper.Instance.Parse24BytesFromString(profiles[i].ip + ":" + profiles[i].port);
                    }
                    byte[] tmpBytes = StructsHelper.Instance.getBytes<MatrixTerminalConfig>(conf);
                    string strConf = BitConverter.ToString(tmpBytes);
                    answer.body.strConfig = strConf;
                }
                return answer;
            }

            return Helper.BuildMessage(what);
        }
        
        
    }
}
