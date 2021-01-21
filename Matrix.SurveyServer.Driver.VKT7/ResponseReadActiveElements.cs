using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseReadActiveElements : ResponseRead
	{
		private List<Element> activeElements = new List<Element>();
		public IEnumerable<Element> ActiveElements
		{
			get
			{
				return activeElements;
			}
		}

		public ResponseReadActiveElements(byte[] data)
			: base(data)
		{

			///0 na
			///1 fn
			///2 len
			///3 data

			var startIndex = 3;
			for (int offset = startIndex; offset < Length + startIndex; offset += 6)
			{
				var address = BitConverter.ToInt16(data, offset + 0);
				var length = BitConverter.ToInt16(data, offset + 4);

				activeElements.Add(new Element(address, length));
			}
		}

		public override string ToString()
		{
			return string.Format("чтение активных элементов");
		}
	}
}
