using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    class FolderHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FolderHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("folder");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid sessionId = Guid.Parse(session.userId);

            if (what == "folders-get")
            {
                var tree = StructureGraph.Instance.GetHierarchy("Folder", "contains", Guid.Parse(session.userId));
                var answer = Helper.BuildMessage(what);
                answer.body.root = tree;
                return answer;
            }

            if (what == "folders-get-2")
            {
                var tree = StructureGraph.Instance.GetFolders(sessionId);                
                var answer = Helper.BuildMessage(what);
                answer.body.root = tree;
                return answer;
            }

            if (what == "folders-by-tubes")
            {
                var tubeIds = new List<Guid>();
                foreach (var tid in message.body.tubeIds)
                {
                    tubeIds.Add(Guid.Parse(tid.ToString()));
                }
                dynamic res = StructureGraph.Instance.GetFoldersTubes(tubeIds.ToArray(), Guid.Parse(session.userId));

                var answer = Helper.BuildMessage(what);
                answer.body.folders = res;
                return answer;
            }

            return Helper.BuildMessage(what);
        }
    }
}
