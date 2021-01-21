//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;

//namespace Matrix.Common.Infrastructure
//{
//    public class Maquette80020
//    {
//        private readonly ConnectionPoint connectionPoint;
//        private readonly ILogger logger;

//        public Maquette80020(ConnectionPoint connectionPoint, ILogger logger)
//        {
//            this.connectionPoint = connectionPoint;
//            this.logger = logger;
//        }

//        public void RegisterRules()
//        {
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(Maquette80020Send).Name, null));
//        }

//        public void Send(Guid maquetteId, IEnumerable<DateTime> days, SessionUser user)
//        {
//            if (logger != null)
//            {
//                logger.NeedLogBy(maquetteId);
//            }
//            connectionPoint.SendMessage(new Maquette80020Send(Guid.NewGuid(), maquetteId, days));
//        }
//    }
//}
