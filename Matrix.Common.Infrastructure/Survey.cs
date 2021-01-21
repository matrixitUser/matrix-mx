//using System;
//using System.Collections.Generic;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;

//namespace Matrix.Common.Infrastructure
//{
//    /// <summary>
//    /// подсистема, отвечающая за опросы данных
//    /// </summary>
//    public class Survey
//    {
//        private readonly ConnectionPoint connectionPoint;

//        public Survey(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;

//        }

//        public void RegisterRules()
//        {
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(PingSourceRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SurveyDailyDataRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SurveyHourlyDataRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SurveyCurrentRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SurveyConstantData).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SurveyAbnormalData).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DoMessage).Name, null));
//        }

//        public void Do(string what, object argument, IEnumerable<Guid> nodeIds)
//        {
//            var message = new DoMessage(Guid.NewGuid(), what, argument, nodeIds);
//            connectionPoint.SendMessage(message);
//        }

//        //public void BeginPing(IEnumerable<Guid> tubeIds, Guid initiatorId)
//        //{
//        //    var message = new PingSourceRequest(Guid.NewGuid(), tubeIds, initiatorId);
//        //    connectionPoint.SendMessage(message);
//        //}

//        //public void SurveyDailyArchive(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, bool onlyHoles, Guid initiatorId)
//        //{
//        //    var message = new SurveyDailyDataRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd, onlyHoles, initiatorId);
//        //    connectionPoint.SendMessage(message);
//        //}

//        //public void SurveyHourlyArchive(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, bool onlyHoles, Guid initiatorId)
//        //{
//        //    //var message = new SurveyHourlyDataRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd, onlyHoles, initiatorId);
//        //    //var message = new DoMessage(Guid.NewGuid(),"Hour", tubeIds, dateStart, dateEnd, onlyHoles, initiatorId);
//        //    //connectionPoint.SendMessage(message);
//        //}

//        //public void SurveyCurrents(IEnumerable<Guid> tubeIds, Guid initiatorId)
//        //{
//        //    connectionPoint.SendMessage(new SurveyCurrentRequest(Guid.NewGuid(), tubeIds, initiatorId));
//        //}

//        //public void SurveyConstants(IEnumerable<Guid> tubeIds, Guid initiatorId)
//        //{
//        //    connectionPoint.SendMessage(new SurveyConstantData(Guid.NewGuid(), tubeIds, initiatorId));
//        //}

//        //public void SurveyAbnormals(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Guid initiatorId)
//        //{
//        //    connectionPoint.SendMessage(new SurveyAbnormalData(Guid.NewGuid(), tubeIds, dateStart, dateEnd, initiatorId));
//        //}
//    }
//}
