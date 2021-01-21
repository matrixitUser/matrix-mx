using System;
using Matrix.Domain.Entities;

namespace Matrix.Common.Infrastructure
{
    public class RegisteredUserDescription
    {
        public User User { get; set; }
        public Group Group { get; set; }
        public Guid? SessionId { get; set; }
    }
}
