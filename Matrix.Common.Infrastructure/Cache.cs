//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Domain.Entities;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using System.Threading;
//using log4net;
//using Matrix.Common.Infrastructure.Authorize;

//namespace Matrix.Common.Infrastructure
//{
//    /// <summary>
//    /// кеш
//    /// отвечает за CRUD кешируемых ентитей (наследников AggregationRoot) на стороне клиента
//    /// </summary>
//    public class Cache : ICache
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(Cache));

//        private Dictionary<Guid, AggregationRoot> cache = new Dictionary<Guid, AggregationRoot>();

//        private readonly ConnectionPoint connectionPoint;

//        public Cache(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;
//        }

//        public string GetFullName(Guid objectId, SessionUser user, int variant = 0)
//        {
//            return AggregationRootHelper.GetFullName(this, objectId, user, variant);
//        }

//        public void BeginLoad()
//        {
//            var response = connectionPoint.SendSyncMessage(new DoMessage(Guid.NewGuid(), "get-entities", null, new Guid[] { }), timeout: 3 * 60 * 1000);
//            if (response != null && response is DoMessage)
//            {
//                var message = response as DoMessage;
//                lock (cache)
//                {
//                    var entities = (IEnumerable<AggregationRoot>)message.Argument["entities"];
//                    cache.Clear();
//                    foreach (var entity in entities)
//                    {
//                        if (!cache.ContainsKey(entity.Id))
//                        {
//                            cache.Add(entity.Id, entity);
//                        }
//                    }
//                    RaiseCacheReloaded();
//                }
//            }
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            var message = e.Message;
//            switch (message.What)
//            {
//                case "entities-updated":
//                    {
//                        var units = (IEnumerable<CacheModelUpdateUnit>)message.Argument["units"];
//                        lock (cache)
//                        {
//                            //обновляем локальный кеш
//                            foreach (var unit in units)
//                            {
//                                switch (unit.ModificationType)
//                                {
//                                    case ModificationType.Added:
//                                    case ModificationType.AccessAllow:
//                                        if (cache.ContainsKey(unit.UpdatedCachedModel.Id)) break;
//                                        cache.Add(unit.UpdatedCachedModel.Id, unit.UpdatedCachedModel);
//                                        break;
//                                    case ModificationType.Deleted:
//                                    case ModificationType.AccessDenied:
//                                        if (!cache.ContainsKey(unit.UpdatedCachedModel.Id)) break;
//                                        cache.Remove(unit.UpdatedCachedModel.Id);
//                                        break;
//                                    case ModificationType.Edited:
//                                        if (!cache.ContainsKey(unit.UpdatedCachedModel.Id))
//                                        {
//                                            cache.Add(unit.UpdatedCachedModel.Id, unit.UpdatedCachedModel);
//                                        }
//                                        else
//                                        {
//                                            cache[unit.UpdatedCachedModel.Id] = unit.UpdatedCachedModel;
//                                        }
//                                        break;
//                                }
//                            }
//                        }
//                        //уведомляем подписчиков
//                        RaiseEntitiesChanged(units);
//                        break;
//                    }
//                default:
//                    {
//                        RaiseDo(message.What, message.Argument, message.NodeIds);
//                        break;
//                    }
//            }
//        }

//        public event EventHandler<DoEventArgs> DoSomething;
//        private void RaiseDo(string what, Dictionary<string, object> argument, IEnumerable<Guid> objectIds)
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

//        /// <summary>
//        /// обновление нескольких сущностей
//        /// </summary>
//        public event EventHandler<EntitiesChangedEventArgs> EntitiesChanged;
//        private void RaiseEntitiesChanged(IEnumerable<CacheModelUpdateUnit> units)
//        {
//            if (EntitiesChanged != null)
//            {
//                ThreadPool.QueueUserWorkItem(state =>
//                {
//                    try
//                    {
//                        EntitiesChanged(this, new EntitiesChangedEventArgs(units));
//                    }
//                    catch (Exception ex)
//                    {
//                        log.Error(string.Format("ошибка при обработке события `изменение кеша`"), ex);
//                    }
//                });
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

//        public IEnumerable<DataRecord> Save(IEnumerable<AggregationRoot> entities, SessionUser user)
//        {
//            if (user == null || user.User == null) return null;
//            var argument = new Dictionary<string, object>
//            {
//                {"entities",entities.ToArray()},
//                {"user",user}
//            };
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), "save-entities", argument, new Guid[] { }));
//            return null;
//        }

//        public void SaveSync(IEnumerable<AggregationRoot> entities, SessionUser user)
//        {
//            if (user == null || user.User == null) return;
//            var argument = new Dictionary<string, object>
//            {
//                {"entities",entities.ToArray()},
//                {"user",user}
//            };
//            connectionPoint.SendSyncMessage(new DoMessage(Guid.NewGuid(), "save-entities", argument, new Guid[] { }));            
//        }

//        public void SaveSync(AggregationRoot entity, SessionUser user)
//        {
//            if (user == null || user.User == null) return;
//            SaveSync(new List<AggregationRoot> { entity }, user);
//        }

//        public IEnumerable<DataRecord> Save(AggregationRoot entity, SessionUser user)
//        {
//            if (user == null || user.User == null) return null;
//            return Save(new List<AggregationRoot> { entity }, user);
//        }

//        /// <summary>
//        /// получает сущности из кеша удовлетворяющие предикату
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <param name="predicate"></param>
//        /// <param name="user"> </param>
//        /// <returns></returns>
//        public IEnumerable<TAggregationRoot> Get<TAggregationRoot>(Func<TAggregationRoot, bool> predicate, SessionUser user) where TAggregationRoot : AggregationRoot
//        {
//            if (user == null) return null;
//            lock (cache)
//            {
//                if (IsCacheOwner(user.User))
//                {
//                    return cache.Values.OfType<TAggregationRoot>().Where(predicate).ToList();
//                }
//                return cache.Values.OfType<TAggregationRoot>().Where(ar => AccessHelper.CanRead(ar, user.Group)).Where(predicate).ToList();
//            }
//        }

//        /// <summary>
//        /// получает все сущности типа TTagged из кеша
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <returns></returns>
//        public IEnumerable<TAggregationRoot> Get<TAggregationRoot>(SessionUser user) where TAggregationRoot : AggregationRoot
//        {
//            if (user == null) return null;
//            lock (cache)
//            {
//                if (IsCacheOwner(user.User))
//                {
//                    return cache.Values.OfType<TAggregationRoot>().ToList();
//                }
//                return cache.Values.OfType<TAggregationRoot>().Where(ar => AccessHelper.CanRead(ar, user.Group)).ToList();
//            }
//        }

//        /// <summary>
//        /// получает сущности из кеша удовлетворяющие предикату
//        /// </summary>
//        /// <typeparam name="TAggregationRoot"></typeparam>
//        /// <param name="predicate"></param>
//        /// <param name="user"> </param>
//        /// <returns></returns>
//        public TAggregationRoot First<TAggregationRoot>(Func<TAggregationRoot, bool> predicate, SessionUser user) where TAggregationRoot : AggregationRoot
//        {
//            lock (cache)
//            {
//                if (user == null) return null;

//                var item = cache.Values.OfType<TAggregationRoot>().FirstOrDefault(predicate);
//                if (AccessHelper.CanRead(item, user.Group))
//                    return item;

//                return null;
//            }
//        }

//        public AggregationRoot ById(Guid id, SessionUser user)
//        {
//            lock (cache)
//            {
//                if (user == null) return null;

//                if (cache.ContainsKey(id))
//                {
//                    var item = cache[id];
//                    if (AccessHelper.CanRead(item, user.Group))
//                        return item;
//                }
//                return null;
//            }
//        }

//        #region DataRecord
//        public void SaveDataRecord(IEnumerable<DataRecord> records, SessionUser user)
//        {
//            if (user == null || user.User == null) return;
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), "save-records", new Dictionary<string, object> { { "records", records } }, null));
//        }
//        public IEnumerable<DataRecord> GetDataRecords(string type, IEnumerable<Guid> objectIds, ArgumentCollection args)
//        {
//            var answer = connectionPoint.SendSyncMessage(new DoMessage(Guid.NewGuid(), "get-records", new Dictionary<string, object> { }, objectIds));
//            if (answer == null) return null;
//            var records = (IEnumerable<DataRecord>)answer.Argument["records"];
//            return records;
//        }
//        public event EventHandler<DataRecordsReceivedEventArgs> DataRecordsReceived;
//        private void RaiseDataRecordsReceived(IEnumerable<DataRecord> records)
//        {
//            if (DataRecordsReceived != null)
//            {
//                DataRecordsReceived(this, new DataRecordsReceivedEventArgs(records));
//            }
//        }
//        #endregion

//        #region Do
//        public void Do(string what, ArgumentCollection args, IEnumerable<Guid> objectIds, SessionUser user)
//        {
//            //todo: включить юзера в сообщение
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), what, args, objectIds));
//        }
//        public void Do(string what, Dictionary<string, object> args, IEnumerable<Guid> objectIds, SessionUser user)
//        {
//            //todo: включить юзера в сообщение
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), what, args, objectIds));
//        }
//        public DoMessage SyncDo(string what, Dictionary<string, object> args, IEnumerable<Guid> objectIds, SessionUser user,int timeout=30000)
//        {
//            //todo: включить юзера в сообщение
//            return connectionPoint.SendSyncMessage(new DoMessage(Guid.NewGuid(), what, args, objectIds),timeout);
//        }
//        #endregion

//        /// <summary>
//        /// Возвращает true, если пользователь находится в самой верхней группе, то есть ему доступен весь кеш
//        /// </summary>
//        /// <param name="user"></param>
//        /// <returns></returns>
//        public bool IsCacheOwner(User user)
//        {
//            if (user == null || !cache.ContainsKey(user.GroupId))
//            {
//                return false;
//            }
//            var group = cache[user.GroupId] as Group;
//            if (group == null) return false;

//            if (group.ParentId.HasValue && cache.ContainsKey(group.ParentId.Value)) return false;

//            return true;
//        }

//        internal void SendDataRecords(IEnumerable<DataRecord> records, SessionUser sessionUser)
//        {
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), "save-records", new Dictionary<string, object> { { "records", records } }, null));
//        }

//        public void Delete(List<Relation> entities, SessionUser user)
//        {

//            if (user == null || user.User == null) return;
//            var argument = new Dictionary<string, object>
//            {
//                {"entities",entities.ToArray()},
//                {"user",user}
//            };
//            connectionPoint.SendMessage(new DoMessage(Guid.NewGuid(), "delete-entities", argument, new Guid[] { }));            
//        }

//        public void DeleteSync(List<Relation> entities, SessionUser user)
//        {

//            if (user == null || user.User == null) return;
//            var argument = new Dictionary<string, object>
//            {
//                {"entities",entities.ToArray()},
//                {"user",user}
//            };
//            connectionPoint.SendSyncMessage(new DoMessage(Guid.NewGuid(), "delete-entities", argument, new Guid[] { }));
//        }
//    }

//    /// <summary>
//    /// изменения в кеше
//    /// </summary>
//    public class EntitiesChangedEventArgs : EventArgs
//    {
//        public IEnumerable<CacheModelUpdateUnit> Units { get; private set; }

//        public EntitiesChangedEventArgs(IEnumerable<CacheModelUpdateUnit> units)
//        {
//            Units = units;
//        }
//    }
//    public class DataRecordsReceivedEventArgs : EventArgs
//    {
//        public IEnumerable<DataRecord> DataRecords { get; private set; }

//        public DataRecordsReceivedEventArgs(IEnumerable<DataRecord> dataRecords)
//        {
//            DataRecords = dataRecords;
//        }
//    }
//    public class DoEventArgs : EventArgs
//    {
//        public string What { get; private set; }
//        public Dictionary<string, object> Argument { get; private set; }
//        public IEnumerable<Guid> ObjectIds { get; private set; }

//        public DoEventArgs(string what, Dictionary<string, object> argument, IEnumerable<Guid> objectIds)
//        {
//            What = what;
//            Argument = argument;
//            ObjectIds = objectIds;
//        }
//    }
//}
