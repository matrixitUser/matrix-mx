using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
	public abstract class Entity
	{
        public virtual Guid Id { get; set; }
        public virtual IList<Tag> Tags { get; set; }

        public Entity()
        {
            Tags = new List<Tag>();
        }
	}
}
