using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Common.Infrastructure.Protocol.Messages
{
    public class DoMessage
    {
        public Guid Id { get; private set; }

        public string What { get; private set; }
        public Dictionary<string, object> Argument { get; private set; }
        public IEnumerable<Guid> NodeIds { get; private set; }

        public DoMessage(Guid id, string what, Dictionary<string, object> argument, IEnumerable<Guid> nodeIds)
        {
            Id = id;
            What = what;
            Argument = argument;
            if (nodeIds == null || !nodeIds.Any())
            {
                NodeIds = new Guid[] { };
            }
            else
            {
                NodeIds = nodeIds.ToList();
            }
        }

        public override string ToString()
        {
            return string.Format("сообщение {0}", What);
        }
    }
}
