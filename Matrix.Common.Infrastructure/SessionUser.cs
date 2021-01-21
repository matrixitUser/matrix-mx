using Matrix.Domain.Entities;
using System;

namespace Matrix.Common.Infrastructure
{
	public class SessionUser
	{
		public SessionUser(User user, Group @group)
		{
			Group = @group;
			User = user;
            Id = user.Id;
		}

		public User User { get; private set; }
		public Group Group { get; private set; }
        public Guid Id { get; private set; }

		public override string ToString()
		{
			return string.Format("{0} [{1}]", User, Group);
		}
	}
}
