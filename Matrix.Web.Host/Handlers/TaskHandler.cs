using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.IO;
using System.Dynamic;
using log4net;
using System.Configuration;

namespace Matrix.Web.Host.Handlers
{
    class TaskHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TaskHandler));
        
        public bool CanAccept(string what)
        {
            return what.StartsWith("task");
        }
        
        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "task-list")
            {
                var objs = StructureGraph.Instance.GetTasks(Guid.Parse(session.user.id));
                var ans = Helper.BuildMessage(what);
                ans.body.objs = objs;
                return ans;
            }

            if (what == "task-get")
            {
                var id = Guid.Parse((string)message.body.id);
                var userId = Guid.Parse(session.user.id);
                //
                var m = StructureGraph.Instance.GetTaskById(id, userId);
                //
                var ans = Helper.BuildMessage(what);
                ans.body.obj = m;
                return ans;
            }
            
            return Helper.BuildMessage(what);
        }

    }
}
