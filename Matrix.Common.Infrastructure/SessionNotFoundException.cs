using System;

namespace Matrix.Common.Infrastructure
{
    public class SessionNotFoundException : Exception
    {
        public Guid SessionId { get; private set; }

        public SessionNotFoundException(Guid sessionId)
        {
            SessionId = sessionId;
        }
        public SessionNotFoundException(Guid sessionId, string message):base(message)
        {
            SessionId = sessionId;
        }
    }
}
