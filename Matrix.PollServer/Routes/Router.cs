using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Nodes;

namespace Matrix.PollServer.Routes
{
    class Router
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Router));

        //// метод реализуется теперь в самом таске
        //public bool BuildFromPath(IEnumerable<PollNodePathWrapper> path, PollTask initiator)
        //{
        //    var localPath = path.ToArray();

        //    var route = new Route();

        //    Func<bool, bool> cleanUp = (success) =>
        //    {
        //        foreach (var n in localPath)
        //        {
        //            n.Node.Unlock(route);
        //        }

        //        //var finals = initiator.GetFinals();
        //        //if (finals != null)
        //        //{
        //        //    foreach (var final in finals)
        //        //    {
        //        //        final.Notify();
        //        //    }
        //        //}
        //        initiator.LastActivityTime = DateTime.Now;

        //        var final = localPath.LastOrDefault();
        //        if (final != null) final.Node.Notify();
        //        return success;
        //    };


        //    route.SetPath(localPath);

        //    foreach (var node in localPath)
        //    {
        //        if (!node.Node.Lock(route))
        //        {
        //            return cleanUp(false);
        //        }
        //    }

        //    foreach (var node in localPath.Reverse())
        //    {
        //        if (!node.Node.Prepare(route, node.Left, initiator))
        //        {
        //            foreach (var n in localPath.SkipWhile(nn => node != nn))
        //            {
        //                n.Node.Release(route, n.Left);
        //            }
        //            return cleanUp(false);
        //        }
        //    }

        //    foreach (var node in localPath)
        //    {
        //        node.Node.Release(route, node.Left);
        //    }
        //    return cleanUp(true);
        //    //});
        //}

        //public void AddTask(PollTask task, IEnumerable<PollNode> nodes)
        //{
        //    log.Debug(string.Format("добавляется таск для {0} нодов", nodes.Count()));
        //    nodes.AsParallel().ForAll(t =>
        //    {
        //        t.AddTask(task);
        //    });
        //    log.Debug(string.Format("добавлен таск для {0} нодов", nodes.Count()));
        //    //var finals = nodes.SelectMany(n => n.Paths.Where(p=>p.Any()).Select(p => p.Last())).Distinct().ToArray();
        //    //log.Debug(string.Format("уведомляются финальные ноды {0}", finals.Length));
        //    ////finals.AsParallel().ForAll(f => f.Notify());
        //    //foreach (var final in finals)
        //    //{
        //    //    final.Node.Notify();
        //    //    //Task.Factory.StartNew(() => final.Notify());
        //    //}
        //}

        public void Destroy(PollNode start, Route route, int port)
        {
            if (start == null) return;
            log.Trace($"[{start}] уничтожение route={route}");
            start.Release(route, port);
            start.Unlock(route);

            Destroy(route.GetNext(start), route, port);
            route.RemoveLast(start);
        }

        private Router() { }
        static Router() { }
        private static Router instance = new Router();
        public static Router Instance
        {
            get { return instance; }
        }
    }

    enum Direction
    {
        FromInitiator,
        ToInitiator
    }
}
