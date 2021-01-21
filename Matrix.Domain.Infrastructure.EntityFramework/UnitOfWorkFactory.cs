//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure;

//namespace Matrix.Domain.Infrastructure.EntityFramework
//{
//    public class UnitOfWorkFactory : IUnitOfWorkFactory
//    {
//        //private static Context context = new Context();

//        public IUnitOfWork Create(bool useTransaction = false)
//        {
//            ITypes mappingType;
//            IBulkInserter bulkInserter;
//            switch (StorageSection.GetSection().MappingType)
//            {
//                case MappingType.MsSql:
//                    mappingType = new MssqlTypes();
//                    bulkInserter = null;// new MsSqlBulkInserter();
//                    break;
//                case MappingType.PostgreSql:
//                    mappingType = new PostgresqlTypes();
//                    bulkInserter = new PostgreSqlBulkInserter();
//                    break;
//                default:
//                    mappingType = new MssqlTypes();
//                    bulkInserter = null;// new MsSqlBulkInserter();
//                    break;
//            }

//            return new UnitOfWork(new Context(mappingType, bulkInserter), useTransaction);
//        }
//    }
//}
