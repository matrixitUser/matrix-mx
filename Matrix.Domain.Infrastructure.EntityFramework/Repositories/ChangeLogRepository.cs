//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Domain.Entities;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class ChangeLogRepository : BaseRepository, IChangeLogRepository
//    {
//        public ChangeLogRepository(Context context) : base(context) { }

//        public IEnumerable<ChangeLog> GetChanges(DateTime dateStart, DateTime dateEnd)
//        {
//            return context.Set<ChangeLog>().Where(s => s.RaiseTime >= dateStart && s.RaiseTime <= dateEnd);
//        }

//        public void Save(ChangeLog log)
//        {
//            var local = context.Set<ChangeLog>().FirstOrDefault(s => s.Id == log.Id);

//            if (local == null)
//            {
//                context.Set<ChangeLog>().Add(log);
//            }
//        }
//    }
//}
