using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class RequestWrite : Request
	{
		public RequestWrite(byte networkAddress, short startRegister, short registerCount, byte[] data)
			: base(networkAddress, 16)
		{
			if (data.Length > byte.MaxValue) throw new Exception("количество байт данных превышает максимально возможное");

			Frame.Add(Helper.GetHighByte(startRegister));
			Frame.Add(Helper.GetLowByte(startRegister));

			Frame.Add(Helper.GetHighByte(registerCount));
			Frame.Add(Helper.GetLowByte(registerCount));

			Frame.Add((byte)data.Length);
			Frame.AddRange(data);
		}
	}
}
