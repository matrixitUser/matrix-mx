using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Data
{
    class ParametersHandler : IRecordHandler
    {
        private static readonly string[] types = new string[] { "Hour", "Day", "Current" };

        public void Handle(IEnumerable<Domain.Entities.DataRecord> records, Guid userId)
        {
            var groups = records.Where(r => types.Contains(r.Type)).GroupBy(r => r.ObjectId).Select(g => new { objectId = g.Key, parameters = g.Select(r => r.S1).Distinct() });

            //var userId = StructureGraph.Instance.GetRootUser();
            foreach (var group in groups)
            {
                //var parameters = StructureGraph.Instance.GetParameters(group.objectId, userId);
                var parameters = CacheRepository.Instance.GetParameters(group.objectId);

                foreach (var parameter in group.parameters)
                {
                    if (!parameters.Any(p => p.name == parameter))
                    {
                        dynamic newParameter = new ExpandoObject();
                        newParameter.name = parameter;
                        newParameter.id = Guid.NewGuid();
                        newParameter.type = "Parameter";
                        //StructureGraph.Instance.SaveParameter(group.objectId, newParameter, userId);

                        //dynamic token = new ExpandoObject();
                        //token.action = "save";
                        //token.start = new ExpandoObject();
                        //token.start.id = group.objectId;
                        //token.start.type = "Tube";
                        //token.end = newParameter;

                        //token.rel = new ExpandoObject();
                        //token.rel.type = "parameter";

                        //token.userId = userId;

                        CacheRepository.Instance.Del("row", group.objectId);
                        CacheRepository.Instance.SaveParameter(group.objectId, newParameter);
                        //StructureGraph.Instance.SavePair(token.start, token.end, token.rel, userId);

                        StructureGraph.Instance.AddNode(newParameter.id, "Parameter", newParameter, StructureGraph.Instance.GetRootUser());
                        StructureGraph.Instance.AddOrUpdRelation(group.objectId, newParameter.id, "parameter", new ExpandoObject(), StructureGraph.Instance.GetRootUser());

                        //Data.NodeBackgroundProccessor.Instance.AddTokens(new dynamic[] { token });
                    }
                }
            }
        }
    }
}
