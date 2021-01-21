//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{
//    public interface IData
//    {
//        void Save(IEnumerable<INonCached> data, Group group);
//        //void GetDailyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Action<IEnumerable<DailyData>> callback, Action<double> onProgress = null);
//        //void GetHourlyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Action<IEnumerable<HourlyData>> callback, Action<double> onProgress = null);
//        //void GetAbnormalEvents(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Action<IEnumerable<AbnormalEvent>> callback, Action<double> onProgress = null);
//        //void GetChangeLog(DateTime dateStart, DateTime dateEnd, Action<IEnumerable<ChangeLog>> callback, Action<double> onProgress = null);

//        void GetDailyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<IEnumerable<DateTime>> callback, Action<double> onProgress = null);
//        void GetHourlyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<IEnumerable<DateTime>> callback, Action<double> onProgress = null);

        
//        //void GetConstantData(IEnumerable<Guid> tubeIds, Action<IEnumerable<ConstantData>> callback, Action<double> onProgress = null);
//        //sync
        
//        //IEnumerable<ChangeLog> GetChangeLog(DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null);

//        //IEnumerable<AbnormalEvent> GetAbnormalEvents(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null);

//        IEnumerable<DateTime> GetDailyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null);
//        IEnumerable<DateTime> GetHourlyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null);

        
//        //IEnumerable<ConstantData> GetConstantData(IEnumerable<Guid> tubeIds, Action<double> onProgress = null);

//        void SaveNew(IEnumerable<DataRecord> data, SessionUser user);
//        IEnumerable<DataRecord> GetNew(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd, bool datesOnly = false);
//        IEnumerable<DataRecord> GetLast(string type, IEnumerable<Guid> objectIds);

//        event EventHandler<DataRecordChangedEventArgs> DataRecordChanged;

//        void SendRandomBytes(IEnumerable<Guid> connectionIds, IEnumerable<byte> bytes);
//    }

//    public class DataRecordChangedEventArgs : EventArgs
//    {
//        public IEnumerable<DataRecord> Records { get; private set; }

//        public DataRecordChangedEventArgs(IEnumerable<DataRecord> records)
//        {
//            Records = records;
//        }
//    }
//}
