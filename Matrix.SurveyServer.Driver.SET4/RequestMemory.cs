using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    class RequestCursor  : Request    //стр 151
	{
		public RequestCursor(byte networkAddress, byte nArray )
            : base(networkAddress)
		{
            byte[] aArray = new byte[3]{0x03,0x08,0x09};
			Data.Add(0x04);//код расширенного запроса на чтение памяти
            Data.Add(0x00);   //идентификатор запроса (любое значение)
            Data.Add(aArray[nArray]);


        }

		public override string ToString()
		{
			return string.Format("чтение памяти");
		}
	}
}
 