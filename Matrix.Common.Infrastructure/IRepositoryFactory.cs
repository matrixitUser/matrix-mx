using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Common.Infrastructure
{
	public interface IRepositoryFactory<TRepository> where TRepository : class
	{
		TRepository Create(IUnitOfWork unitOfWork);
	}
}
