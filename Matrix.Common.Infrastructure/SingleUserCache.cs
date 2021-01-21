//using log4net;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using Matrix.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;

//namespace Matrix.Common.Infrastructure
//{
//    /// <summary>
//    /// обертка над кешем, 
//    /// связан с пользователем, ассоциирует все методы с ним
//    /// </summary>
//    public class SingleUserCache
//    {
//        private Cache cache;
//        private User user;
//        private Group group;
//        private SessionUser sessionUser;
//        public SessionUser SessionUser { get { return sessionUser; } }
//        public SingleUserCache(ConnectionPoint connectionPoint)
//        {
//            cache = new Cache(connectionPoint);
//            cache.EntitiesChanged += EntitiesChangedHandler;
//            cache.CacheReloaded += CacheReloadedHandler;
//            cache.DataRecordsReceived += CacheDataRecordsReceived;
//            cache.DoSomething += (se, ea) => RaiseDo(ea.What, ea.Argument, ea.ObjectIds);
//        }

//        public event EventHandler<DoEventArgs> DoSomething;
//        private void RaiseDo(string what, Dictionary<string,object> argument, IEnumerable<Guid> objectIds)
//        {
//            if (DoSomething != null)
//            {
//                try
//                {
//                    DoSomething(this, new DoEventArgs(what, argument, objectIds));
//                }
//                catch (Exception ex)
//                {

//                }
//            }
//        }

//        void CacheDataRecordsReceived(object sender, DataRecordsReceivedEventArgs e)
//        {
//            RaiseDataRecordsReceived(e.DataRecords);
//        }

//        void CacheReloadedHandler(object sender, EventArgs e)
//        {
//            RaiseCacheReloaded();
//        }

//        void EntitiesChangedHandler(object sender, EntitiesChangedEventArgs e)
//        {
//            RaiseEntitiesChanged(e.Units);
//        }

//        public void Init(User user, Group group)
//        {            
//            this.user = user;
//            this.group = group;
//            sessionUser = new SessionUser(user, group);
//            cache.BeginLoad();
//        }

//        public string GetFullName(Guid objectId, int variant = 0)
//        {
//            return cache.GetFullName(objectId, sessionUser, variant);
//        }

//        /// <summary>
//        /// обновление нескольких сущностей
//        /// </summary>
//        public event EventHandler<EntitiesChangedEventArgs> EntitiesChanged;
//        private void RaiseEntitiesChanged(IEnumerable<CacheModelUpdateUnit> units)
//        {
//            if (EntitiesChanged != null)
//            {
//                EntitiesChanged(this, new EntitiesChangedEventArgs(units));
//            }
//        }

//        /// <summary>
//        /// перезагрузка кеша
//        /// происходит при старте, логауте и т.п. операциях
//        /// </summary>
//        public event EventHandler CacheReloaded;
//        private void RaiseCacheReloaded()
//        {
//            if (CacheReloaded != null)
//            {
//                CacheReloaded(this, EventArgs.Empty);
//            }
//        }

//        public void Save(IEnumerable<AggregationRoot> entities)
//        {
//            cache.Save(entities, sessionUser);
//        }

//        public void Save(AggregationRoot entity)
//        {
//            cache.Save(entity, sessionUser);
//        }

//        /// <summary>
//        /// получает сущности из кеша удовлетворяющие предикату
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <param name="predicate"></param>
//        /// <param name="user"> </param>
//        /// <returns></returns>
//        public IEnumerable<TAggregationRoot> Get<TAggregationRoot>(Func<TAggregationRoot, bool> predicate) where TAggregationRoot : AggregationRoot
//        {
//            return cache.Get<TAggregationRoot>(predicate, sessionUser);
//        }

//        /// <summary>
//        /// получает все сущности типа TTagged из кеша
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <returns></returns>
//        public IEnumerable<TAggregationRoot> Get<TAggregationRoot>() where TAggregationRoot : AggregationRoot
//        {
//            return cache.Get<TAggregationRoot>(sessionUser);
//        }

//        /// <summary>
//        /// получает сущности из кеша удовлетворяющие предикату
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <param name="predicate"></param>
//        /// <param name="user"> </param>
//        /// <returns></returns>
//        public TAggregationRoot First<TAggregationRoot>(Func<TAggregationRoot, bool> predicate) where TAggregationRoot : AggregationRoot
//        {
//            return cache.First<TAggregationRoot>(predicate, sessionUser);
//        }

//        public AggregationRoot ById(Guid id)
//        {
//            return cache.ById(id, sessionUser);
//        }

//        #region DataRecord
//        public void SaveDataRecord(IEnumerable<DataRecord> records)
//        {
//            cache.SaveDataRecord(records, sessionUser);
//        }
//        public IEnumerable<DataRecord> GetDataRecords(string type, IEnumerable<Guid> objectIds, ArgumentCollection args)
//        {
//            return cache.GetDataRecords(type, objectIds, args);
//        }
//        public IEnumerable<DataRecord> GetDataRecords(string type, ArgumentCollection args)
//        {
//            return GetDataRecords(type, null, args);
//        }
//        public event EventHandler<DataRecordsReceivedEventArgs> DataRecordsReceived;
//        private void RaiseDataRecordsReceived(IEnumerable<DataRecord> records)
//        {
//            if (DataRecordsReceived != null)
//            {
//                DataRecordsReceived(this, new DataRecordsReceivedEventArgs(records));
//            }
//        }
//        public void SendDataRecords(IEnumerable<DataRecord> records)
//        {
//            cache.SendDataRecords(records, sessionUser);
//        }
//        #endregion

//        #region Do
//        public void Do(string what, ArgumentCollection args, IEnumerable<Guid> objectIds)
//        {
//            cache.Do(what, args, objectIds, sessionUser);
//        }
//        public void Do(string what, Dictionary<string,object> args, IEnumerable<Guid> objectIds)
//        {
//            cache.Do(what, args, objectIds, sessionUser);
//        }
//        public DoMessage SyncDo(string what, Dictionary<string, object> args, IEnumerable<Guid> objectIds)
//        {
//            return cache.SyncDo(what, args, objectIds, sessionUser);
//        }
//        #endregion
//    }
//}
