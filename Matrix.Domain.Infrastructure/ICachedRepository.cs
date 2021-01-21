//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using Matrix.Domain.Entities;

//namespace Matrix.Domain.Infrastructure
//{
//    public interface ICachedRepository
//    {
//        IEnumerable<AggregationRoot> GetAll();
//        IEnumerable<T> GetAll<T>() where T : AggregationRoot;
//        T GetById<T>(Guid id) where T : Tagged;
//        IEnumerable<T> Get<T>(Expression<Func<T, bool>> exp) where T : AggregationRoot;
//        void Save<T>(T cached) where T : AggregationRoot;
//        void Add(ChangeLog log);
//        void Delete<T>(T cached) where T : Tagged;
//        IEnumerable<string> GetTags(string name);
//    }
//}
