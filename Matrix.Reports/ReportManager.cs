using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    public class ReportManager
    {
        public string Build(Guid reportId,DateTime start,DateTime end,IEnumerable<Guid> targets)
        {

            dynamic model = new ExpandoObject();

            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];
            
            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                //var res = sess.Run("match(t:Tube {id:{tubeId}})-->(d:Device)<--(dr:Driver) return t, dr.driver as driver", new { tubeId = tubeId.ToString() });
                //d = res.Select(r => new { t = r["t"].As<INode>().ToDynamic(), driver = r["driver"].As<string>() }).FirstOrDefault();
            }

            //model.targets = StructureGraph.Instance.GetRows(targets, userId).ToArray();
            //model.start = start;
            //model.end = end;

            //var report = StructureGraph.Instance.GetNodeById(reportId, userId);

            //var result = Reports.Mapper.Instance.Map(model, report.template, session);

            //return result;

            return "hahahahha";
        }
    }
}
