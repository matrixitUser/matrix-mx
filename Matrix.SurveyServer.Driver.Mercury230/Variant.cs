using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
	{
        dynamic ParseVariantResponse(dynamic answer)
		{
            if (!answer.success) return answer;
            
			byte variant = (byte)(answer.Body[1] & 0x0F);
            answer.mem3 = (byte)((answer.Body[3] >> 7) & 0x01);
            answer.A = vars[variant];

            return answer;
		}

		Dictionary<byte, int> vars = new Dictionary<byte, int>()
		{
			{0,5000},
			{1,25000},
			{2,1250},
			{3,500},
			{4,1000},
			{5,250}
		};
	}
}
