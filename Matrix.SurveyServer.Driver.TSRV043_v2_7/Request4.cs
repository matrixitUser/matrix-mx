using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV043
{
	class Request4 : Request
	{
        public int StartAddress { get; private set; }
		public int RegisterCount { get; private set; }

        public Request4(byte networkAddress, int startAddress, int registerCount)
			: base(networkAddress, 4)
		{
			StartAddress = startAddress;
			RegisterCount = registerCount;
			Data.Add(Helper.GetHighByte(startAddress));
            Data.Add(Helper.GetLowByte(startAddress));
			Data.Add(Helper.GetHighByte(registerCount));
			Data.Add(Helper.GetLowByte(registerCount));
		}

		public override string ToString()
		{
			return string.Format("запрос на чтение регистров, начальный адрес: {0:X}, количество: {1}", StartAddress, RegisterCount);
		}
	}
}
