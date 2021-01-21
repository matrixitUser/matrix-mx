using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseReadPropertiesMultiplier : ResponseRead
	{
		//public static IEnumerable<Element> Elements { get; set; }

		private List<FracElement> fracs = new List<FracElement>();
		public IEnumerable<FracElement> Fracs
		{
			get
			{
				return fracs;
			}
		}

		public ResponseReadPropertiesMultiplier(byte[] data, IEnumerable<Element> elements)
			: base(data)
		{
			var elementIndex = 0;
			var offset = 3;

			while (offset < Length && elementIndex < elements.Count())
			{
				var element = elements.ElementAt(elementIndex);
				var frac = data[offset];

				fracs.Add(new FracElement(frac, element.Address));

				elementIndex++;
				offset += 1 + 2;
			}
		}
	}

	public class FracElement
	{
		public byte Frac { get; private set; }
		public short Address { get; private set; }

		public short AddressReference { get; private set; }

		public FracElement(byte frac, short address)
		{
			Frac = frac;
			Address = address;

			switch (Address)
			{
				case 1:
					AddressReference = 1;
					break;
			}
		}

		public override string ToString()
		{
			return string.Format("addr={0}({1}) frac={2}", Address, AddressReference, Frac);
		}
	}
}
