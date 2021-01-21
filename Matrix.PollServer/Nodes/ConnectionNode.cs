using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes
{
    //static class PollType
    //{
    //    public const char Hour = 'h';
    //    public const char Day = 'd';
    //}

    enum PollType
    {
        Hour = 'h',
        Day = 'd'
    }

    abstract class ConnectionNode : PollNode
    {
        /// <summary>
        /// период опроса через данное соединение
        /// </summary>
        /// <returns></returns>
        public virtual dynamic GetPeriod()
        {
            var period = ParsePeriod();
            if (period != null)
                return period;
            else
                return GetDefaultPeriod();
        }

        protected dynamic ParsePeriod()
        {
            var dcontent = content as IDictionary<string, object>;
            if (dcontent.ContainsKey("period"))
            {
                dynamic period = new ExpandoObject();

                string sPeriod = content.period.ToString();
                if (string.IsNullOrEmpty(sPeriod) || sPeriod.Length < 2) return null;

                char sType = sPeriod[0];
                switch (sType)
                {
                    case (char)PollType.Day: period.type = PollType.Day; break;
                    case (char)PollType.Hour: period.type = PollType.Hour; break;
                    default: return null;
                }

      
                int value = 0;
                if (int.TryParse(content.period.ToString().Substring(1), out value))
                {
                    period.value = value;
                    return period;
                }
            }

            return null;
        }

        protected virtual dynamic GetDefaultPeriod()
        {
            //значение по-умолчанию
            dynamic period = new ExpandoObject();
            period.type = PollType.Day;
            period.value = 1;

            return period;
        }
    }
}
