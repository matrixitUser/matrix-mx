using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseReadPropertiesUnits : ResponseRead
	{
		//public static IEnumerable<Element> Elements { get; set; }

		private List<UnitElement> units = new List<UnitElement>();
		public IEnumerable<UnitElement> Units
		{
			get
			{
				return units;
			}
		}

		public ResponseReadPropertiesUnits(byte[] data, IEnumerable<Element> elements)
			: base(data)
		{
			var elementIndex = 0;
			var offset = 3;

			while (offset < Length || elementIndex < elements.Count())
			{
				var element = elements.ElementAt(elementIndex);

				var len = BitConverter.ToUInt16(data, offset);

				var unit = Encoding.GetEncoding(866).GetString(data, offset + 2, len);

				units.Add(new UnitElement(unit, element.Address));

				elementIndex++;
				offset += 2 + len + 2;
			}
		}
	}

	public class UnitElement
	{
		public string Unit { get; private set; }
		public short Address { get; private set; }

		public UnitElement(string unit, short address)
		{
			Unit = unit;
			Address = address;
		}

		public override string ToString()
		{
			return string.Format("addr={0} unit={1}", Address, Unit);
		}
	}
}
