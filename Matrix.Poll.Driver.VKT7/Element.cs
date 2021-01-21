using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.VKT7
{
	//public class FuckingElement
	//{
	//    public short Address { get; private set; }

	//    public FuckingElement(short address)
	//    {
	//        Address = address;
	//    }

	//    //public static IEnumerable<UnitProperty> GetUnitProperties()
	//    //{
	//    //    List<UnitProperty> elements = new List<UnitProperty>();
	//    //    for (short address = 44; address < 56; address++)
	//    //    {
	//    //        elements.Add(new Element(address, 7));
	//    //    }
	//    //    return elements;
	//    //}
	//}

	//public class Parameter : FuckingElement
	//{
	//    public FracProperty FracProperty { get; set; }
	//    public UnitProperty UnitProperty { get; set; }


	//    public short Length { get; private set; }
	//}

	//public class FracProperty : FuckingElement
	//{
	//    public byte Frac { get; set; }
	//}

	//public class UnitProperty
	//{
	//    public string Unit { get; set; }




	//}

	public class Element //: Foo
	{
		public short Address { get; private set; }
		public short Length { get; private set; }
		protected List<byte> Data { get; private set; }

		public Element(short address, short length)
		{
			Address = address;
			Length = length;

			Data = new List<byte>();

			Data.Add(Helper.GetLowByte(Address));
			Data.Add(Helper.GetHighByte(Address));

			var delimeter = 0x4000;
			Data.Add(Helper.GetLowByte(delimeter));
			Data.Add(Helper.GetHighByte(delimeter));

			Data.Add(Helper.GetLowByte(Length));
			Data.Add(Helper.GetHighByte(Length));
		}

		public byte[] GetBytes()
		{
			return Data.ToArray();
		}

		public override string ToString()
		{
			return string.Format("addr={0}, len={1}", Address, Length);
		}

		public static IEnumerable<Element> GetUnitProperties()
		{
			List<Element> elements = new List<Element>();
			for (short address = 44; address < 56; address++)
			{
				elements.Add(new Element(address, 7));
			}
			return elements;
		}

		public static IEnumerable<Element> GetMultiplierProperties()
		{
			List<Element> elements = new List<Element>();
			for (short address = 57; address < 77; address++)
			{
				elements.Add(new Element(address, 1));
			}
			return elements;
		}

		public static IEnumerable<Element> GetParameters()
		{
			List<Element> elements = new List<Element>();
			for (short address = 44; address < 56; address++)
			{
				elements.Add(new Element(address, 7));
			}
			return elements;
		}
	}

	public static class ElementExtensions
	{
		public static byte[] GetBytes(this IEnumerable<Element> elements)
		{
			List<byte> bytes = new List<byte>();
			foreach (var element in elements)
			{
				bytes.AddRange(element.GetBytes());
			}
			return bytes.ToArray();
		}
	}
}
