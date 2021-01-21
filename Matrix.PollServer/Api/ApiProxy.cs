using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Api
{
    class ApiProxy
    {
        public void SaveStates(IEnumerable<dynamic> states)
        {
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic saveMsg = new ExpandoObject();
            saveMsg.head = new ExpandoObject();
            saveMsg.body = new ExpandoObject();

            saveMsg.head.what = "node-states";
            saveMsg.body.states = states;
            connector.SendMessage(saveMsg);
        }

        public void VCom(dynamic body)
        {
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic saveMsg = new ExpandoObject();
            saveMsg.head = new ExpandoObject();
            saveMsg.body = new ExpandoObject();

            saveMsg.head.what = "poll-vcom-response";
            saveMsg.body = body;
            connector.SendMessage(saveMsg);
        }

        public void SaveIndication(double indication, string indicatioUnitMeasurement, DateTime date, Guid objectId)
        {
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic saveMsg = new ExpandoObject();
            saveMsg.head = new ExpandoObject();
            saveMsg.body = new ExpandoObject();

            saveMsg.head.what = "node-value";
            saveMsg.body.indication = indication;
            saveMsg.body.indicatioUnitMeasurement = indicatioUnitMeasurement;
            saveMsg.body.date = date;
            saveMsg.body.objectId = objectId;
            connector.SendMessage(saveMsg);
        }


        private ApiProxy() { }
        static ApiProxy() { }
        private static readonly ApiProxy instance = new ApiProxy();
        public static ApiProxy Instance
        {
            get
            {
                return instance;
            }
        }

        //internal void SaveNode(dynamic node)
        //{
        //    var connector = UnityManager.Instance.Resolve<IConnector>();
        //    dynamic saveMsg = new ExpandoObject();
        //    saveMsg.head = new ExpandoObject();
        //    saveMsg.body = new ExpandoObject();

        //    saveMsg.head.what = "node-save";
        //    saveMsg.body.node = node;
        //    //connector.SendMessage(saveMsg);
        //}

        //public void SaveNodes(IEnumerable<dynamic> nodes)
        //{
        //    dynamic message = new ExpandoObject();
        //    message.head = new ExpandoObject();
        //    message.body = new ExpandoObject();

        //    message.head.what = "nodes-save";
        //    message.body.nodes = nodes.ToArray();

        //    var connector = UnityManager.Instance.Resolve<IConnector>();
        //    var answer = connector.SendMessage(message);
        //}
    }
}
