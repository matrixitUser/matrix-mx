using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// запись с данными
    /// </summary>
    public class DataRecord : ICloneable
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public Guid ObjectId { get; set; }

        public double? D1 { get; set; }
        public double? D2 { get; set; }
        public double? D3 { get; set; }

        public int? I1 { get; set; }
        public int? I2 { get; set; }
        public int? I3 { get; set; }

        public string S1 { get; set; }
        public string S2 { get; set; }
        public string S3 { get; set; }

        public DateTime? Dt1 { get; set; }
        public DateTime? Dt2 { get; set; }
        public DateTime? Dt3 { get; set; }

        public Guid? G1 { get; set; }
        public Guid? G2 { get; set; }
        public Guid? G3 { get; set; }

        public object Clone()
        {
            return new DataRecord
            {
                Id = Id,
                Type = Type,
                Date = Date,
                ObjectId = ObjectId,
                D1 = D1,
                D2 = D2,
                D3 = D3,
                I1 = I1,
                I2 = I2,
                I3 = I3,
                S1 = S1,
                S2 = S2,
                S3 = S3,
                Dt1 = Dt1,
                Dt2 = Dt2,
                Dt3 = Dt3,
                G1 = G1,
                G2 = G2,
                G3 = G3,
            };
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} = {2} {3}",Date, S1,D1,S2);
        }
    }
}
