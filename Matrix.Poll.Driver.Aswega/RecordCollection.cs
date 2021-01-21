using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SA94
{
    class RecordCollection: List<Record>
    {
        public bool ContainsKey(Address address)
        {
            return this.Any(r => r.Address == address);
        }

        public Record GetNearestRecordHA(DateTime date)
        {
            double min = double.MaxValue;
            Address minKey = null;
            foreach (var pair in this)
            {
                var current = Math.Abs((pair.Block1.DateTime - date).TotalHours);
                if (current < min)
                {
                    min = current;
                    minKey = pair.Address;
                }
                current = Math.Abs((pair.Block2.DateTime - date).TotalHours);
                if (current < min)
                {
                    min = current;
                    minKey = pair.Address;
                }
            }
            return this.FirstOrDefault(k => k.Address == minKey);
        }
        
        public Record GetNearestRecordDA(DateTime date)
        {
            double min = double.MaxValue;
            Address minKey = null;
            foreach (var pair in this)
            {
                var current = Math.Abs((pair.Block1.DateTime - date).TotalDays);
                if (current < min)
                {
                    min = current;
                    minKey = pair.Address;
                }
                current = Math.Abs((pair.Block2.DateTime - date).TotalDays);
                if (current < min)
                {
                    min = current;
                    minKey = pair.Address;
                }
            }
            return this.FirstOrDefault(k => k.Address == minKey);
        }
        
        public bool IsRecordExist(DateTime dateTime)
        {
            foreach (var record in this)
            {
                if ((dateTime == record.Block1.DateTime) || (dateTime == record.Block2.DateTime) || (dateTime == record.Block1.Date) || (dateTime == record.Block2.Date))
                {
                    return true;
                }
            }
            return false;
        }

        public Record ExistRecord(DateTime dateTime)
        {
            foreach (var record in this)
            {
                if ((dateTime == record.Block1.DateTime) || (dateTime == record.Block2.DateTime) || (dateTime == record.Block1.Date) || (dateTime == record.Block2.Date))
                {
                    return record;
                }
            }
            return null;
        }
        
    }
}
