using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Responses;

namespace Matrix.Scheduler
{
    public class ApiModule : NancyModule
    {

        public ApiModule(TaskManager tm)
        {
            Get[""] = (_) =>
            {
                return Response.AsFile("ui/index.html");
            };

            Get["tasks"] = (_) =>
            {
                return Response.AsJson(tm.Tasks);
            };

            Get["tubes/{taskId}"] = (p) =>
            {
                Guid tid = Guid.Parse(p.taskId.ToString());
                var ids = tm.GetTaskTubeIds(tid);
                return Response.AsJson(ids);
            };

            Get["restart"] = (_) =>
            {
                tm.Stop();
                tm.Start();
                return "restarted";
            };

            Get["stop"] = (_) =>
            {
                tm.Stop();
                return "stopped";
            };


            Get["start"] = (_) =>
            {
                tm.Start();
                return "started";
            };
        }
    }
}
