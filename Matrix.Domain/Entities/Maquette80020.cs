using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
	/// <summary>
	/// макет 80020 для отправки в сбытовую компанию
	/// доп. параметры в тегах	
	/// </summary>
	[DataContract]
	public class Maquette80020 :Entity// AggregationRoot
	{         
		[DataMember]
		public string Name { get; set; }

		public const string NUMBER = "Number";
		public const string SMTP_SERVER = "SmtpServer";
		public const string SMTP_PORT = "SmtpPort";
		public const string MAIL_FROM = "MailFrom";
		public const string MAIL_TO = "MailTo";
		public const string MAIL_PASSWORD = "MailPassword";
		public const string INN = "Inn";
		public const string BINDING_TUBES = "BindingTubes";
		public const string SENDER = "Sender";

		public const string TASK_TAG = "TASK_TAG";

		public override string ToString()
		{
			return Name;
		}
	}
}
