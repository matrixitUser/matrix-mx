using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common.Crc
{
	public class Crc16Modbus : ICrcCalculator
	{
		private ushort[] table = null;
		private bool isBackward = false;

		public int CrcDataLength { get { return 2; } }

		public Crc16Modbus(bool isBackward)
		{
			this.isBackward = isBackward;

			ushort polynomial = 0xA001;
			table = new ushort[256];
			ushort value;
			ushort temp;

			for (ushort i = 0; i < table.Length; i++)
			{
				value = 0;
				temp = i;
				for (byte j = 0; j < 8; j++)
				{
					if (((value ^ temp) & 0x0001) != 0)
					{
						value = (ushort)((value >> 1) ^ polynomial);
					}
					else
					{
						value >>= 1;
					}
					temp >>= 1;
				}
				table[i] = value;
			}
		}

		public Crc16Modbus() : this(false) { }

		public Crc Calculate(byte[] buffer, int offset, int length)
		{
			ushort crc = 0xFFFF;

			if (isBackward)
			{
				for (int i = length - 1; i >= offset; i--)
				{
					byte index = (byte)(crc ^ buffer[i]);
					crc = (ushort)((crc >> 8) ^ table[index]);
				}
			}
			else
			{
				for (int i = offset; i < length; i++)
				{
					byte index = (byte)(crc ^ buffer[i]);
					crc = (ushort)((crc >> 8) ^ table[index]);
				}
			}

			Crc crc16 = new Crc(new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) });

			return crc16;
		}
	}
}
