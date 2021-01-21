//using System;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class DataRecordRepositoryFactory : IDataRecordRepositoryFactory
//    {
//        public IDataRecordRepository Create(Common.Infrastructure.IUnitOfWork unitOfWork)
//        {
//            var uow = unitOfWork as UnitOfWork;
//            if (uow == null)
//            {
//                throw new ArgumentException("unitOfWork должен быть производным от UnitOfWork");
//            }

//            return new DataRecordRepository(uow.Context);
//        }
//    }
//}
