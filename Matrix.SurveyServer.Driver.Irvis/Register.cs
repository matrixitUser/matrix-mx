using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
	public class Register
	{
		public byte HightByte { get; private set; }
		public byte LowByte { get; private set; }

		public byte[] Bytes
		{
			get
			{
				return new byte[]
				{
					HightByte,
					LowByte
				};
			}
		}

		public Register(short raw)
		{
			HightByte = Helper.GetHighByte(raw);
			LowByte = Helper.GetLowByte(raw);
		}
		
		/// <summary>
		/// регистр выбора страницы
		/// см. документацию п. 4, стр. 7
		/// </summary>
		public static Register SelectPageRegister
		{
			get
			{
				return new Register(0x84);
			}
		}
	}
}
