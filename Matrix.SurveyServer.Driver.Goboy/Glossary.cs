using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    static class Glossary
    {
        public static string V_norm { get { return "нормальный объем"; } }
        public static string V_work { get { return "рабочий объем"; } }
        public static string P { get { return "давление"; } }
        public static string T { get { return "температура"; } }
        public static string NWTime { get { return "нерабочее время"; } }
        public static string Rate { get { return V_work; } }
        public static string NormRate { get { return V_norm; } }
        public static string TimeError { get { return NWTime; } }
        public static string Acc { get { return "признак ошибки по питанию"; } }
    }
}
