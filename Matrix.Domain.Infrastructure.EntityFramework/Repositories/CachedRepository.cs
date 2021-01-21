//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Runtime.Serialization;
//using Matrix.Common.Infrastructure.Authorize;
//using Matrix.Domain.Entities;
//using log4net;
//using System.Data.Entity.Infrastructure;
//using System.Data;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    /// <summary>
//    /// репозиторий для tagged ентитей
//    /// todo каждому корню аггрегации - по репозиторию
//    /// </summary>
//    public class CachedRepository : BaseRepository, ICachedRepository
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(CachedRepository));

//        public CachedRepository(Context context) : base(context) { }

//        public IEnumerable<IEntity> GetAll()
//        {
//            //some butthurt here

//            var result = new List<IEntity>(context.Set<IEntity>().Count());
//            //result.AddRange(context.Set<Prototype>().Include("Tags"));
//            result.AddRange(context.Set<Group>().Include("Tags"));
//            result.AddRange(context.Set<User>().Include("Tags"));
//            result.AddRange(context.Set<DeviceType>().Include("Tags"));
//            result.AddRange(context.Set<Task>().Include("Tags"));
//            result.AddRange(context.Set<Report>().Include("Tags"));
//            result.AddRange(context.Set<Maquette80020>().Include("Tags"));
//            var foo = context.Set<Node>().Include("Tags").ToList();
//            result.AddRange(context.Set<Node>().Include("Tags"));
//            result.AddRange(context.Set<Relation>().Include("Tags"));
//            result.AddRange(context.Set<TubeParameter>().Include("Tags"));
//            result.AddRange(context.Set<GsmModem>().Include("Tags"));
//            return result;
//        }

//        public IEnumerable<T> GetAll<T>() where T : IEntity
//        {
//            return context.Set<T>().ToList();
//        }

//        public T GetById<T>(Guid id) where T : IEntity
//        {
//            return context.Set<T>().FirstOrDefault(s => s.Id == id);
//        }

//        public IEnumerable<T> Get<T>(Expression<Func<T, bool>> exp) where T : IEntity
//        {
//            return context.Set<T>().Where(exp).ToList();
//        }

//        private void Attach<TEntity>(TEntity entity) where TEntity : IEntity
//        {
//            if (entity == null) return;
//            var incomingTags = entity.Tags.ToList();

//            var isNew = !context.Set<TEntity>().Any(t => t.Id == entity.Id);
//            if (isNew)
//            {
//                context.Set<TEntity>().Add(entity);
//            }
//            else
//            {
//                context.Entry(entity).State = System.Data.Entity.EntityState.Modified;
//            }

//            var oldTags = context.Set<Tag>().Where(t => t.TaggedId == entity.Id).ToList();

//            var removedTags = oldTags.Where(t => !incomingTags.Select(tg => tg.Id).Contains(t.Id));
//            var updatedTags = oldTags.Where(t => incomingTags.Select(tg => tg.Id).Contains(t.Id));
//            var addedTags = incomingTags.Where(t => !oldTags.Select(tg => tg.Id).Contains(t.Id));
//            foreach (var removedTag in removedTags)
//            {
//                //context.Entry(removedTag).State = EntityState.Deleted;
//                context.Set<Tag>().Remove(removedTag);
//            }
//            foreach (var updatedTag in updatedTags)
//            {
//                context.Entry(updatedTag).State = System.Data.Entity.EntityState.Modified;
//            }
//            foreach (var addedTag in addedTags)
//            {
//                context.Set<Tag>().Add(addedTag);
//                //context.Entry(addedTag).State = EntityState.Added;                
//            }
//        }

//        public void Save<T>(T aggregationRoot) where T : AggregationRoot
//        {
//            Attach(aggregationRoot);
//            return;
//        }

//        public void Add(ChangeLog changeLog)
//        {
//            context.Set<ChangeLog>().Add(changeLog);
//        }

//        public void Delete<T>(T tagged) where T : Tagged
//        {
//            var local = GetById<T>(tagged.Id);
//            if (local == null)
//            {
//                log.DebugFormat("сущность не была найдена в базе, удаления не было, {0}", tagged);
//                return;
//            }
//            context.Set<T>().Remove(local);
//            log.DebugFormat("сущность была удалена, {0}", tagged);
//        }

//        public IEnumerable<string> GetTags(string name)
//        {
//            return context.Set<Tag>().Where(t => t.Name == name).Select(t => t.Value).Distinct();
//        }
//    }
//}