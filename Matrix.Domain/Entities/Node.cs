using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Matrix.Domain.Entities
{
    [DataContract]    
    public class Node : Entity, INode
    {


        [DataMember]
        public string Type { get; set; }

        public string ToLongString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Узел (тип={0};", Type);
            foreach (var tag in Tags)
            {
                sb.AppendFormat("{0}={1};", tag.Name, tag.Value);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public override string ToString()
        {
            return string.Format("{0}", Type);
        }
    }
}
