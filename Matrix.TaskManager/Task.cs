using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.TaskManager
{
    public class PollTask
    {
        public Guid TargetId { get; private set; }
        public List<Route> Routes { get; set; }
        public dynamic Details { get; set; }

        public PollTask(Guid targetId)
        {
            TargetId = targetId;
            Routes = new List<Route>();
        }
    }

    public class Route
    {
        public string PortName { get; set; }
        public Guid[] Points { get; set; }
        public int Priority { get; private set; }
        public RouteState State { get; set; }

        public Route(string portName, int priority, Guid[] points)
        {
            PortName = portName;
            Priority = priority;
            Points = points;
            State = RouteState.Wait;
        }
    }

    public enum RouteState
    {
        Success,
        Wait,
        Reject,
        InProccess
    }
}
