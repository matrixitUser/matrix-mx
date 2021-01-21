using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761.Protocol
{
	class Field : IBytes
	{
		public const byte HT = 0x09;

		public byte[] Data { get; set; }

		public string Text
		{
			get
			{
				var encoding = Encoding.GetEncoding(866);
				return encoding.GetString(Data.ToArray());
			}
			set
			{
				var encoding = Encoding.GetEncoding(866);
				Data = encoding.GetBytes(value);
			}
		}

		public Field()
		{
		}

		public Field(string field)
		{
			var encoding = Encoding.GetEncoding(866);
			this.Data = encoding.GetBytes(field);
		}

		public IEnumerable<byte> GetBytes()
		{
			List<byte> bytes = new List<byte>();

			bytes.Add(HT);
			bytes.AddRange(Data);

			return bytes;
		}

		public override string ToString()
		{
			var encoding = Encoding.GetEncoding(866);
			return encoding.GetString(Data);
		}
	}
}
