using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
	/// <summary>
	/// запрос тарифного расписания
	/// </summary>
    public partial class Driver
	{
		byte[] MakeTariffScheduleRequest(UInt16 address, byte count)
		{
            var Data = new List<byte>();
			byte strange = 1;

			byte addressBit = 0;
			byte energyType = 0;
			byte memory = 2;

			strange = (byte)(((addressBit & 0x01) << 7) | ((energyType & 0x07) << 4) | (memory & 0xf));

			Data.Add(strange);
			Data.Add(Helper.GetHighByte(address));
			Data.Add(Helper.GetLowByte(address));
			Data.Add(count);
            
            return MakeBaseRequest(0x06, Data);
		}
	}
}
