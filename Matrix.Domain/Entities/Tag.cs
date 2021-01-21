using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// тег - дополнительное описание сущности
    /// расширяет сущность
    /// </summary>
    [DataContract]
    [Serializable]
    public class Tag
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public Guid TaggedId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Value { get; set; }
        [DataMember]
        public bool IsSpecial { get; set; }        
    }
}
