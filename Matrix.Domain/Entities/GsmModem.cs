using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class GsmModem : Entity
    {
        [DataMember]
        public string ComPort { get; set; }
        [DataMember]
        public int BaudRate { get; set; }
        [DataMember]
        public Guid CsdPortId { get; set; }
    }
}
