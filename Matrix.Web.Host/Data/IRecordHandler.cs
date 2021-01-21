using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;

namespace Matrix.Web.Host.Data
{
    interface IRecordHandler
    {
        void Handle(IEnumerable<DataRecord> records, Guid userId);
    }
}
