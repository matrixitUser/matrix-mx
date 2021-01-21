//using System;
//using System.Collections.Generic;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{
//    public interface ICache
//    {
//        IEnumerable<TAggregationRoot> Get<TAggregationRoot>(Func<TAggregationRoot, bool> predicate, SessionUser user) where TAggregationRoot : IEntity;
//        TAggregationRoot First<TAggregationRoot>(Func<TAggregationRoot, bool> predicate, SessionUser user) where TAggregationRoot : IEntity;
//        IEntity ById(Guid id, SessionUser user);
//        IEnumerable<TAggregationRoot> Get<TAggregationRoot>(SessionUser user) where TAggregationRoot : IEntity;
//        IEnumerable<DataRecord> Save(IEnumerable<IEntity> entities, SessionUser user);
//        IEnumerable<DataRecord> Save(IEntity entity, SessionUser user);
//        string GetFullName(Guid entityId, SessionUser user, int variant = 0);
//        event EventHandler<EntitiesChangedEventArgs> EntitiesChanged;
//        event EventHandler CacheReloaded;
//        event EventHandler<DoEventArgs> DoSomething;
//    }
//}
