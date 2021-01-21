using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class RightsRule
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Guid GroupId { get; set; }
        [DataMember]
        public Guid ObjectId { get; set; }
        [DataMember]
        public Guid? RelyId { get; set; }
    }
}
