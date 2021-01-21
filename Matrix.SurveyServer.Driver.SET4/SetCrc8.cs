using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.SET4
{
	class SetCrc8 : ICrcCalculator
	{
		public int CrcDataLength
		{
			get { return 1; }
		}

		public Crc Calculate(byte[] buffer, int offset, int length)
		{
			byte crc = 0xFF;

			for (int i = offset; i < length; i++)
			{
				crc =(byte) (crc ^ buffer[i]);
			}

			return new Crc(new byte[]{crc});
		}
	}
}
