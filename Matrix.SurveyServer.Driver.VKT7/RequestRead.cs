using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	///чтение регистров
	///см. док. п. 3.1. стр. 11	
	/// </summary>
	public class RequestRead : Request
	{
		public RequestRead(byte networkAddress, short startRegister, short registerCount)
			: base(networkAddress, 0x03)
		{
			Frame.Add(Helper.GetHighByte(startRegister));	//начальный регистр
			Frame.Add(Helper.GetLowByte(startRegister));

			Frame.Add(Helper.GetHighByte(registerCount));	//количество регистров
			Frame.Add(Helper.GetLowByte(registerCount));
		}
	}
}
