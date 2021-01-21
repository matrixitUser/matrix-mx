using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
{
    public class StructureRepository
    {
        /// <summary>
        /// имя строки соединеия
        /// </summary>
        const string CS_NAME = "Context";

        private string ConnectionString { get { return ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString; } }

        private readonly Context context;

        /// <summary>
        /// фоновый контекст, для подгрузки данных без влияния на основной контекст
        /// </summary>
        private readonly Context bgContext;

        public StructureRepository()
        {
            context = new Context(ConnectionString);
            bgContext = new Context(ConnectionString);
        }

        public void RemoveRelation(Relation relation)
        {
            context.Entry(relation).State = System.Data.Entity.EntityState.Deleted;
            foreach (var tag in context.Set<Tag>().Where(t => t.TaggedId == relation.Id))
            {
                context.Entry(tag).State = System.Data.Entity.EntityState.Deleted;
            }
            context.SaveChanges();
        }

        public IEnumerable<Node> GetNodes()
        {
            return context.Set<Node>().Include("Tags").ToList();
        }

        public IEnumerable<Relation> GetRelations()
        {
            return context.Set<Relation>().Include("Tags").ToList();
        }

        public IEnumerable<User> GetUsers()
        {
            return context.Set<User>().Include("Tags").ToList();
        }

        public IEnumerable<Group> GetGroups()
        {
            return context.Set<Group>().Include("Tags").ToList();
        }

        public IEnumerable<GsmModem> GetModems()
        {
            return context.Set<GsmModem>().Include("Tags").ToList();
        }

        public IEnumerable<DeviceType> GetDrivers()
        {
            return context.Set<DeviceType>().Include("Tags").ToList();
        }

        public void AddEntity<TEntity>(TEntity entity) where TEntity : Entity
        {
            context.Entry(entity).State = System.Data.Entity.EntityState.Added;
            context.SaveChanges();
        }

        public void UpdateEntity<TEntity>(TEntity entity) where TEntity : Entity
        {
            //1. старые теги
            var oldTags = bgContext.Set<Tag>().Where(t => t.TaggedId == entity.Id).ToList();
            //2. новые теги
            var newTags = entity.Tags.ToList();

            var modifiedTags = newTags.Where(n => oldTags.Select(o => o.Id).Contains(n.Id));
            var deletedTags = oldTags.Where(o => !newTags.Select(n => n.Id).Contains(o.Id));
            var addedTags = newTags.Where(n => !oldTags.Select(o => o.Id).Contains(n.Id));

            foreach (var modifiedTag in modifiedTags)
            {
                var old = oldTags.FirstOrDefault(o => o.Id == modifiedTag.Id);
                old = modifiedTag;
                context.Entry(modifiedTag).State = System.Data.Entity.EntityState.Modified;
            }
            foreach (var deletedTag in deletedTags)
            {
                context.Entry(deletedTag).State = System.Data.Entity.EntityState.Deleted;
            }

            foreach (var addedTag in addedTags)
            {
                context.Entry(addedTag).State = System.Data.Entity.EntityState.Added;
            }

            entity.Tags.Clear();

            context.Entry(entity).State = System.Data.Entity.EntityState.Modified;
            context.SaveChanges();
        }

        public IEnumerable<Report> GetReports()
        {
            return context.Set<Report>().Include("Tags").ToList();
        }

        public void AddRightsRule(Guid groupId, Guid objectId, Guid? relyId)
        {
            var rule = new RightsRule()
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                ObjectId = objectId,
                RelyId = relyId
            };
            context.Set<RightsRule>().Add(rule);
            context.SaveChanges();
        }

        public void RemoveRightsRule(Guid groupId, Guid objectId)
        {
            var rule = context.Set<RightsRule>().FirstOrDefault(r => r.ObjectId == objectId && r.GroupId == groupId);
            if (rule != null)
            {
                if (rule.RelyId.HasValue)
                {
                    var relies = context.Set<RightsRule>().Where(r => r.RelyId == rule.RelyId);
                    foreach (var rely in relies)
                    {
                        context.Entry(rely).State = System.Data.Entity.EntityState.Deleted;
                    }
                }
                context.Entry(rule).State = System.Data.Entity.EntityState.Deleted;
            }

            context.SaveChanges();
        }

        public IEnumerable<RightsRule> GetRightsRules()
        {
            return context.Set<RightsRule>().ToList();
        }
    }
}
