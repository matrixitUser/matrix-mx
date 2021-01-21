//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Common.Infrastructure.Authorize;
//using Matrix.Domain.Entities;
//using log4net;

//namespace Matrix.Common.Infrastructure
//{
//    public abstract class BaseProxy
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(BaseProxy));

//        private readonly ConcurrentDictionary<Guid, IEntity> cacheDictionary = new ConcurrentDictionary<Guid, IEntity>();
//        public IEnumerable<IEntity> GetCache()
//        {
//            return new List<IEntity>(cacheDictionary.Values);
//        }

//        #region внутрение методы для работы со списком
//        protected void AddRange(IEnumerable<IEntity> cache)
//        {
//            if (cache == null) return;
//            foreach (var cached in cache)
//            {
//                if (cacheDictionary.ContainsKey(cached.Id)) continue;
//                cacheDictionary.TryAdd(cached.Id, cached);
//            }
//        }
//        protected void Add(IEntity cached)
//        {
//            if (cached == null) return;
//            if (cacheDictionary.ContainsKey(cached.Id)) return;
//            cacheDictionary.TryAdd(cached.Id, cached);
//        }
//        public bool ContainsModel(IEntity cached)
//        {
//            return cached != null && ContainsModel(cached.Id);
//        }

//        public bool ContainsModel(Guid id)
//        {
//            return cacheDictionary.ContainsKey(id);
//        }
//        /// <summary>
//        /// Удаляет модель из списка и возвращает удаленную модель
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        protected IEntity TakeModel(Guid id)
//        {
//            IEntity cached;
//            cacheDictionary.TryRemove(id, out cached);
//            return cached;
//        }
//        protected void RemoveModel(Guid id)
//        {
//            IEntity cached;
//            cacheDictionary.TryRemove(id, out cached);
//        }
//        protected void RemoveModel(IEntity model)
//        {
//            if (model == null) return;
//            RemoveModel(model.Id);
//        }
//        protected void ClearCache()
//        {
//            cacheDictionary.Clear();
//        }
//        #endregion

//        #region Get methods
//        public IEntity GetCachedModelById(Guid id)
//        {
//            IEntity result = null;
//            try
//            {
//                result = cacheDictionary[id];
//            }
//            catch (Exception)
//            {
//            }
//            return result;
//        }
//        public TCachedModel GetFirstOrDefault<TCachedModel>(Func<TCachedModel, bool> predicate) where TCachedModel : IEntity
//        {
//            foreach (var pair in cacheDictionary)
//            {
//                var typed = (TCachedModel)pair.Value;
//                if (typed == null) continue;

//                if (predicate(typed))
//                    return typed;
//            }
//            return default(TCachedModel);
//        }
//        public IEnumerable<TCachedModel> GetCachedModels<TCachedModel>(Func<TCachedModel, bool> predicate) where TCachedModel : IEntity
//        {
//            //log.DebugFormat("всего в кеше {0} сущностей", cacheDictionary.Count);
//            foreach (var pair in cacheDictionary)
//            {
//                var typed = (TCachedModel)pair.Value;
//                if (typed == null) continue;

//                if (predicate(typed))
//                {
//                    //log.DebugFormat("найдена сущность удовлетворяющая предикату {0}", typed);
//                    yield return typed;
//                }
//            }
//        }
//        public IEnumerable<TCachedModel> GetCachedModels<TCachedModel>() where TCachedModel : IEntity
//        {
//            foreach (var pair in cacheDictionary)
//            {
//                var typed = (TCachedModel)pair.Value ;
//                if (typed == null) continue;

//                yield return typed;
//            }
//        }
//        public IEnumerable<IEntity> GetCachedModels(Type type)
//        {
//            foreach (var pair in cacheDictionary)
//            {
//                var currentType = pair.Value.GetType();
//                if (currentType == type || currentType.IsSubclassOf(type))
//                    yield return pair.Value;
//            }
//        }
//        #endregion
//        #region Access
//        #region Obsolete
//        //public AccessModifier GetRights(Cached cachedObject, Group group, IEnumerable<Cached> additionalObjects = null)
//        //{
//        //    var cache = new Dictionary<Guid, Cached>(cacheDictionary);

//        //    if(additionalObjects!=null)
//        //    {
//        //        foreach (var cached in additionalObjects)
//        //        {
//        //            if(!cache.ContainsKey(cached.Id))
//        //                cache.Add(cached.Id, cached);
//        //        }
//        //    }

//        //    return AccessHelper.GetRights(cachedObject, group, cache);
//        //}
//        #endregion
//        public IEnumerable<IEntity> GetAllowedObject(Group group)
//        {
//            return AccessHelper.GetAllowObjects(group, this);
//        }
//        #region Obsolete
//        //public EditInfo CanCreate(Cached cachedObject, Group group, IEnumerable<Cached> additionalObjects = null)
//        //{
//        //    var cache = new Dictionary<Guid, Cached>(cacheDictionary);
//        //    if(additionalObjects!=null)
//        //        foreach (var additionalObject in additionalObjects)
//        //        {
//        //            cache.Add(additionalObject.Id, additionalObject);
//        //        }

//        //    return AccessHelper.CanCreate(cachedObject, group, cache);
//        //}
//        //public EditInfo CanEdit(Cached editedObject, Cached oldObject, Group group, IEnumerable<Cached> additionalObjects = null)
//        //{
//        //    var cache = new Dictionary<Guid, Cached>(cacheDictionary);
//        //    if (additionalObjects != null)
//        //        foreach (var additionalObject in additionalObjects)
//        //        {
//        //            if(!cache.ContainsKey(additionalObject.Id))
//        //                cache.Add(additionalObject.Id, additionalObject);
//        //        }

//        //    return AccessHelper.CanEdit(editedObject, oldObject, group, cache);
//        //}
//        #endregion
//        #endregion

//        public virtual User GetUser(Guid sessionId)
//        {
//            return null;
//        }

//        //public string GetFullName(Tagged cached)
//        //{
//        //    if (cached == null) return string.Empty;
//        //    if (cached is Tube)
//        //    {
//        //        string res = string.Empty;
//        //        var tube = cached as Tube;

//        //        var area = GetCachedModelById(tube.AreaId) as Area;
//        //        if (area != null)
//        //        {
//        //            res = area.GetFullName();
//        //        }
//        //        //if (string.IsNullOrEmpty(tube.Name))
//        //        //{
//        //        //    res += tube.Name;
//        //        //}

//        //        var hasOtherTubes = GetCachedModels<Tube>(t => t.AreaId == tube.AreaId).Count() > 1;
//        //        if (hasOtherTubes)
//        //        {
//        //            res += " - " + tube.Name;
//        //        }

//        //        if (tube.DeviceTypeId.HasValue)
//        //        {
//        //            var dt = GetCachedModelById(tube.DeviceTypeId.Value) as DeviceType;
//        //            if (dt != null)
//        //            {
//        //                res += string.Format(" [{0}]", dt.DisplayName);
//        //            }
//        //        }

//        //        return res;
//        //    }
//        //    if (cached is User)
//        //    {
//        //        return (cached as User).FullName;
//        //    }
//        //    return cached.ToString();
//        //}
//    }
//}
