using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    class ModemsHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("modem");
        }

        class Comparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x.Length > y.Length)
                    return 1;
                if (x.Length < y.Length)
                    return -1;

                return string.Compare(x, y);
            }
        }


        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid userId = Guid.Parse(session.userId.ToString());

            if (what == "modems-get-all")
            {
                var answer = Helper.BuildMessage(what);
                answer.body.modems = new List<dynamic>();


                var modems = StructureGraph.Instance.GetNodesByType("Modem", userId);

                foreach (var modem in modems.OrderBy(x => (string)x.port, new Comparer()))
                {
                    answer.body.modems.Add(modem);
                }
                return answer;
            }

            //if (what == "modems-of-pool")
            //{
            //    var answer = Helper.BuildMessage(what);
            //    answer.body.modems = new JArray();
            //    Guid poolId = message.body.poolId;
            //    var entities = Cache.Instance.GetEntities((Guid)session.User.id);
            //    foreach (var modem in entities.OfType<GsmModem>().Where(m => m.CsdPortId == poolId))
            //    {
            //        answer.body.modems.Add(modem.ToJSONDynamic());
            //    }
            //    return answer;
            //}

            //if (what == "modems-save")
            //{
            //    var modems = new List<GsmModem>();
            //    foreach (var modem in message.body.modems)
            //    {
            //        modems.Add((GsmModem)EntityExtensions.ToEntity(modem));
            //    }
            //    Cache.Instance.SaveEntities(modems, session.User);
            //}

            return Helper.BuildMessage(what);
        }
    }
}
