using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class Relation :Entity//  AggregationRoot
    {      
        [DataMember]
        public Guid StartNodeId { get; set; }
        [DataMember]
        public Guid EndNodeId { get; set; }
    }
}
