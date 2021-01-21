using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// даты
    /// </summary>
    public class DataRecordDate
    {
        public DateTime Date { get; set; }
        public Guid ObjectId { get; set; }
        public string Type { get; set; }
    }
}
