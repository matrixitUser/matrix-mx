using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761.Protocol
{
	class Category : IBytes
	{
		public const byte FF = 0x0C;

		public List<Field> Fields { get; set; }

		public Category()
		{
			Fields = new List<Field>();
		}

		public IEnumerable<byte> GetBytes()
		{
			List<byte> bytes = new List<byte>();

			Fields.ToList().ForEach(f => bytes.AddRange(f.GetBytes()));
			bytes.Add(FF);

			return bytes;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", string.Join("|", Fields));
		}

		public static Category Parse(byte[] data, int offset, int length)
		{
			var category = new Category();

			var encoding = Encoding.GetEncoding(866);
			var stringData = encoding.GetString(data, offset, length);
			var fields = stringData.Split(new char[] { (char)Field.HT });
			fields.Skip(1).ToList().ForEach(f => category.Fields.Add(new Field(f)));

			return category;
		}
	}
}
