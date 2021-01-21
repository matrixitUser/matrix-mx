//using System;
//using Matrix.Common.Infrastructure;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class ServerSettingRepositoryFactory:IServerSettingRepositoryFactory
//    {
//        public IServerSettingRepository Create(IUnitOfWork unitOfWork)
//        {
//            var uow = unitOfWork as UnitOfWork;
//            if (uow == null)
//            {
//                throw new ArgumentException("unitOfWork должен быть производным от UnitOfWork");
//            }

//            return new ServerSettingRepository(uow.Context);
//        }
//    }
//}
