using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// узлы-саттелиты 
    /// </summary>
    public class SubNode : Entity
    {
        public string Type { get; set; }
        public Guid NodeId { get; set; }
    }
}
