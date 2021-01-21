using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace Matrix.Domain.Entities
{
    [DataContract]
    [Serializable]
    public class DeviceType : Entity
    {        
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
        [DataMember]
        public byte[] Driver { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
