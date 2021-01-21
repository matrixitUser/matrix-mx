//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Transactions;
//using Matrix.Common.Infrastructure;

//namespace Matrix.Domain.Infrastructure.EntityFramework
//{
//    public class UnitOfWork : IUnitOfWork
//    {
//        private readonly TransactionScope transactionScope;
//        private readonly bool useTransaction;
//        private Context context = null;

//        public Context Context
//        {
//            get
//            {
//                return context;
//            }
//        }

//        public UnitOfWork(Context context, bool useTransaction)
//        {
//            this.context = context;
//            this.useTransaction = useTransaction;
//            if (useTransaction)
//            {
//                transactionScope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0, 30, 0));
//            }
//        }

//        public void Commit()
//        {
//            context.SaveChanges();
//            if (useTransaction && transactionScope != null)
//            {
//                transactionScope.Complete();
//            }
//        }

//        public void Dispose()
//        {
//            context.Dispose();

//            if (useTransaction && transactionScope != null)
//            {
//                transactionScope.Dispose();
//            }
//        }
//    }
//}
