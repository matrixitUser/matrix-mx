using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class Group : Entity// AggregationRoot
    {
        [DataMember]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [DataMember]
        public Guid? ParentId { get; set; }

        [DataMember]
        public string Code { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
