//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{
//    /// <summary>
//    /// подсистема работы с архивными данными
//    /// </summary>
//    public class Data : IData
//    {
//        private readonly ConnectionPoint connectionPoint;

//        public Data(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            dataCouple = new Couple<SaveDataRecordsMessage, DataRecord>(records => new SaveDataRecordsMessage(records), 10000, 500);
//            dataCouple.OnCoupleMessageReady += (se, ea) =>
//            {
//                connectionPoint.SendMessage(ea.Message);
//            };
//            connectionPoint.MessageRecieved += OnMessageRecieved;
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            if (e.Message is DataRecordChanged)
//            {
//                var message = e.Message as DataRecordChanged;
//                RaiseDataRecordChanged(message.Records);
//            }
//        }

//        public void RegisterRules()
//        {
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(NonCachedDataResponse).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SaveNonCachedRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DailyDataRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(HourlyDataRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(AbnormalEventsRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(ChangeLogRequest).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(ConstantsRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataTotalsRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataTotalsResponse).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(BuildReportRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(BuildReportResponse).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(ConvertReportRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(ConvertReportResponse).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataRecordsRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataRecordsResponse).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(SaveDataRecordsMessage).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataRecordChanged).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(RandomBytesMessage).Name, null));
//        }

//        #region async
//        public void GetDailyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd,
//             Action<IEnumerable<DailyData>> callback, Action<double> onProgress)
//        {
//            var request = new DataRecordsRequest(DataRecordTypes.DayRecordType, tubeIds, new ArgumentCollection { { DataRecordRequestTypes.DateStart, dateStart }, { DataRecordRequestTypes.DateStart, dateEnd } });

//            connectionPoint.SendMessage(request, response =>
//            {
//                var dailyDataResponse = response as DataRecordsResponse;
//                if (dailyDataResponse != null)
//                {
//                    callback(dailyDataResponse.Records.Select(r => new DailyData()
//                    {
//                        Id = r.Id,
//                        DateReceive = r.Dt1.Value,
//                        DateStart = r.Date,
//                        MeasuringUnitId = r.S2,
//                        SystemParameterId = r.S1,
//                        TubeId = r.ObjectId,
//                        Value = r.D1.Value
//                    }));
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        public void GetHourlyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd,
//            Action<IEnumerable<HourlyData>> callback, Action<double> onProgress = null)
//        {
//            //var request = new HourlyDataRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd);
//            var request = new DataRecordsRequest(DataRecordTypes.HourRecordType, tubeIds, new ArgumentCollection { { DataRecordRequestTypes.DateStart, dateStart }, { DataRecordRequestTypes.DateEnd, dateEnd } });
//            connectionPoint.SendMessage(request, response =>
//            {
//                var nonCachedResponse = response as DataRecordsResponse;
//                if (nonCachedResponse != null)
//                {
//                    callback(nonCachedResponse.Records.Select(r => new HourlyData()
//                    {
//                        Id = r.Id,
//                        DateReceive = r.Dt1.Value,
//                        DateStart = r.Date,
//                        MeasuringUnitId = r.S2,
//                        SystemParameterId = r.S1,
//                        TubeId = r.ObjectId,
//                        Value = r.D1.Value
//                    }));
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        public void GetAbnormalEvents(IEnumerable<Guid> tubeIds, DateTime dateStart,
//            DateTime dateEnd, Action<IEnumerable<AbnormalEvent>> callback, Action<double> onProgress = null)
//        {
//            var request = new DataRecordsRequest(DataRecordTypes.AbnormalRecordType, tubeIds, new ArgumentCollection { { DataRecordRequestTypes.DateStart, dateStart }, { DataRecordRequestTypes.DateEnd, dateEnd } });
//            connectionPoint.SendMessage(request, response =>
//            {
//                var nonCachedResponse = response as DataRecordsResponse;
//                if (nonCachedResponse != null)
//                {
//                    callback(nonCachedResponse.Records.Select(r => new AbnormalEvent()
//                    {
//                        Id = r.Id,
//                        DateStart = r.Date,
//                        Description = r.S1,
//                        DurationMinutes = r.I1.Value,
//                        TubeId = r.ObjectId
//                    }));
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        public void GetChangeLog(DateTime dateStart, DateTime dateEnd,
//            Action<IEnumerable<ChangeLog>> callback, Action<double> onProgress = null)
//        {   
//            connectionPoint.SendMessage(request, response =>
//            {
//                var nonCachedResponse = response as DataRecordsResponse;
//                if (nonCachedResponse != null)
//                {
//                    callback(nonCachedResponse.Records.Select(r => new ChangeLog()
//                    {
//                        Id = r.Id,
//                        Message = r.S1,
//                        ObjectId = r.ObjectId,
//                        ObjectName = r.S2,
//                        RaiseTime = r.Date,
//                        UserId = r.G1.Value,
//                        UserName = r.S3
//                    }));
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        public void GetDailyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd,
//            Action<IEnumerable<DateTime>> callback, Action<double> onProgress = null)
//        {
//            var response = connectionPoint.SendSyncMessage(new DailyHolesRequest(Guid.NewGuid(), tubeId, dateStart, dateEnd), onProgress) as DatesResponse;
//            if (response != null)
//            {
//                callback(response.Dates);
//            }
//        }
//        public void GetHourlyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd,
//            Action<IEnumerable<DateTime>> callback, Action<double> onProgress = null)
//        {
//            var response = connectionPoint.SendSyncMessage(new HourlyHolesRequest(Guid.NewGuid(), tubeId, dateStart, dateEnd), onProgress) as DatesResponse;
//            if (response != null)
//            {
//                callback(response.Dates);
//            }
//        }
//        public void GetConstantData(IEnumerable<Guid> tubeIds, Action<IEnumerable<ConstantData>> callback, Action<double> onProgress = null)
//        {
//            //var request = new ConstantsRequest(Guid.NewGuid(), tubeIds);
//            var request = new DataRecordsRequest(Guid.NewGuid(), DataRecord.ConstantRecordType, tubeIds, DateTime.MinValue, DateTime.MaxValue);
//            connectionPoint.SendMessage(request, response =>
//            {
//                var dailyDataResponse = response as DataRecordsResponse;
//                if (dailyDataResponse != null)
//                {
//                    callback(dailyDataResponse.Records.Select(c => new ConstantData()
//                    {
//                        Id = c.Id,
//                        DateReceive = c.Date,
//                        Name = c.S1,
//                        Value = c.S2,
//                        TubeId = c.ObjectId
//                    }));
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        public void GetTotalData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd,
//            Action<TotalResult> callback, Action<double> onProgress = null)
//        {
//            var request = new DataTotalsRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd);
//            connectionPoint.SendMessage(request, response =>
//            {
//                var totalsResponse = response as DataTotalsResponse;
//                if (totalsResponse != null)
//                {
//                    callback(totalsResponse.TotalResult);
//                    return;
//                }

//                callback(null);
//            }, onProgress);
//        }
//        #endregion

//        public void Save(IEnumerable<INonCached> data, Group group)
//        {
//            //var message = new SaveNonCachedRequest(Guid.NewGuid(), data);
//            //var message = new SaveNonCachedRequest(Guid.NewGuid(), data);
//            //connectionPoint.SendMessage(message);
//        }


//        #region sync

//        public IEnumerable<DateTime> GetDailyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null)
//        {
//            var request = new DailyHolesRequest(Guid.NewGuid(), tubeId, dateStart, dateEnd);
//            var response = connectionPoint.SendSyncMessage(request, onProgress) as DatesResponse;

//            return response != null ? response.Dates : null;
//        }

//        public IEnumerable<DateTime> GetHourlyHoles(Guid tubeId, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null)
//        {
//            var request = new HourlyHolesRequest(Guid.NewGuid(), tubeId, dateStart, dateEnd);
//            var response = connectionPoint.SendSyncMessage(request, onProgress) as DatesResponse;

//            return response != null ? response.Dates : null;
//        }

//        public IEnumerable<DailyData> GetDailyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd,
//             Action<double> onProgress = null)
//        {
//            //var request = new DailyDataRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd);
//            var request = new DataRecordsRequest(DataRecordTypes.DayRecordType, tubeIds, new ArgumentCollection { { DataRecordRequestTypes.DateStart, dateStart }, { DataRecordRequestTypes.DateEnd, dateEnd } });
//            var dailyDataResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataRecordsResponse;

//            return dailyDataResponse != null ? dailyDataResponse.Records.Select(r => new DailyData()
//            {
//                Id = r.Id,
//                DateReceive = r.Dt1.Value,
//                DateStart = r.Date,
//                MeasuringUnitId = r.S2,
//                SystemParameterId = r.S1,
//                TubeId = r.ObjectId,
//                Value = r.D1.Value
//            }) : null;
//        }
//        public IEnumerable<HourlyData> GetHourlyData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd,
//            Action<double> onProgress = null)
//        {
//            var request = new DataRecordsRequest(Guid.NewGuid(), DataRecord.HourRecordType, tubeIds, dateStart, dateEnd);
//            var nonCachedResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataRecordsResponse;

//            return nonCachedResponse == null ? null : nonCachedResponse.Records.Select(r => new HourlyData()
//            {
//                Id = r.Id,
//                DateReceive = r.Dt1.Value,
//                DateStart = r.Date,
//                MeasuringUnitId = r.S2,
//                SystemParameterId = r.S1,
//                TubeId = r.ObjectId,
//                Value = r.D1.Value
//            });
//        }
//        public IEnumerable<AbnormalEvent> GetAbnormalEvents(IEnumerable<Guid> tubeIds, DateTime dateStart,
//            DateTime dateEnd, Action<double> onProgress = null)
//        {
//            //var request = new AbnormalEventsRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd);
//            var request = new DataRecordsRequest(Guid.NewGuid(), DataRecord.AbnormalRecordType, tubeIds, dateStart, dateEnd);
//            var nonCachedResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataRecordsResponse;

//            return nonCachedResponse == null ? null : nonCachedResponse.Records.Select(r => new AbnormalEvent()
//            {
//                Id = r.Id,
//                DateStart = r.Date,
//                Description = r.S1,
//                DurationMinutes = r.I1.Value,
//                TubeId = r.ObjectId
//            });
//        }
//        public IEnumerable<ChangeLog> GetChangeLog(DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null)
//        {
//            //var request = new ChangeLogRequest(Guid.NewGuid(), dateStart, dateEnd);
//            var request = new DataRecordsRequest(Guid.NewGuid(), DataRecord.ChangeLogType, null, dateStart, dateEnd);
//            var nonCachedResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataRecordsResponse;

//            return nonCachedResponse == null ? null : nonCachedResponse.Records.Select(r => new ChangeLog()
//            {
//                Id = r.Id,
//                Message = r.S1,
//                ObjectId = r.ObjectId,
//                ObjectName = r.S2,
//                RaiseTime = r.Date,
//                UserId = r.G1.Value,
//                UserName = r.S3
//            });
//        }

//        public IEnumerable<ConstantData> GetConstantData(IEnumerable<Guid> tubeIds, Action<double> onProgress = null)
//        {
//            //var request = new ConstantsRequest(Guid.NewGuid(), tubeIds);
//            var request = new DataRecordsRequest(Guid.NewGuid(), DataRecord.ConstantRecordType, tubeIds, DateTime.MinValue, DateTime.MaxValue);
//            var dailyDataResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataRecordsResponse;

//            return dailyDataResponse == null ? null : dailyDataResponse.Records.Select(c => new ConstantData()
//            {
//                Id = c.Id,
//                DateReceive = c.Date,
//                Name = c.S1,
//                Value = c.S2,
//                TubeId = c.ObjectId
//            });
//        }
//        public TotalResult GetTotalData(IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Action<double> onProgress = null)
//        {
//            var request = new DataTotalsRequest(Guid.NewGuid(), tubeIds, dateStart, dateEnd);
//            var totalsResponse = connectionPoint.SendSyncMessage(request, onProgress) as DataTotalsResponse;

//            return totalsResponse == null ? null : totalsResponse.TotalResult;
//        }
//        #endregion

//        private Couple<SaveDataRecordsMessage, DataRecord> dataCouple;
//        public void SaveNew(IEnumerable<DataRecord> data, SessionUser user)
//        {
//            dataCouple.Add(data);
//            //connectionPoint.SendMessage(new DataRecordsSave(Guid.NewGuid(), data));
//        }

//        public IEnumerable<DataRecord> GetNew(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd, bool datesOnly = false)
//        {
//            var response = connectionPoint.SendSyncMessage(new DataRecordsRequest(type, objectIds, dateStart, dateEnd, datesOnly));
//            if (response != null && response is DataRecordsResponse)
//            {
//                return (response as DataRecordsResponse).Records;
//            }
//            return null;
//        }


//        public IEnumerable<DataRecord> GetLast(string type, IEnumerable<Guid> objectIds)
//        {
//            var response = connectionPoint.SendSyncMessage(new DataRecordsLastRequest(Guid.NewGuid(), type, objectIds));
//            if (response != null && response is DataRecordsResponse)
//            {
//                return (response as DataRecordsResponse).Records;
//            }
//            return null;
//        }


//        public event EventHandler<DataRecordChangedEventArgs> DataRecordChanged;

//        private void RaiseDataRecordChanged(IEnumerable<DataRecord> records)
//        {
//            if (DataRecordChanged != null)
//            {
//                try
//                {
//                    DataRecordChanged(this, new DataRecordChangedEventArgs(records));
//                }
//                catch (Exception ex)
//                {

//                }
//            }
//        }

//        public void SendRandomBytes(IEnumerable<Guid> connectionIds, IEnumerable<byte> bytes)
//        {
//            connectionPoint.SendMessage(new RandomBytesMessage(Guid.NewGuid(), connectionIds, bytes));
//        }
//    }
//}
