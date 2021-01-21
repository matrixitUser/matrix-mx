//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;
//using System.Data.Common;

//namespace Matrix.Domain.Infrastructure
//{
//    /// <summary>
//    /// инсертит множество записей за раз
//    /// 
//    /// </summary>
//    public interface IBulkInserter
//    {
//        /// <summary>
//        /// внимание!
//        /// TEntity должен быть указан явно 
//        /// </summary>
//        /// <typeparam name="TEntity"></typeparam>
//        /// <param name="entities"></param>
//        /// <param name="connection"></param>
//        void Insert<TEntity>(IEnumerable<TEntity> entities, DbConnection connection);
//    }
//}
