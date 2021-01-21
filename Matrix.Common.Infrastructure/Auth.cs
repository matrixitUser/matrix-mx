using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Infrastructure.Protocol;
using Matrix.Domain.Entities;

namespace Matrix.Common.Infrastructure
{
	/// <summary>
	/// модуль авторизации
	/// отвечает за аторизацию
	/// </summary>
	public class Auth
	{
		private readonly ConnectionPoint connectionPoint;
		//private readonly ICache cache;

		public Auth(ConnectionPoint connectionPoint)
		{
			this.connectionPoint = connectionPoint;
			this.connectionPoint.MessageRecieved += OnMessageRecieved;
		}

		private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
		{

		}

		public void Login(string login, string password, Action<bool> callback)
		{

		}

		public void Logout()
		{

		}

		public bool CanSee(Guid id)
		{
			return false;
		}

		public bool CanEdit(Guid id)
		{
			return false;
		}

		public User User { get; private set; }
		public Group Group { get; private set; }
	}
}
