using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{	
    public partial class Driver
	{
        dynamic ParseVersionResponse(dynamic answer)
		{
            if(!answer.success) return answer;

            answer.Version = new Version(Helper.FromBCD(answer.Body[0]), Helper.FromBCD(answer.Body[1]), Helper.FromBCD(answer.Body[2]));
            return answer;
		}
	}

	class Version
	{
		public int First { get; private set; }
		public int Second { get; private set; }
		public int Third { get; private set; }

		public Version(int first, int second, int third)
		{
			First = first;
			Second = second;
			Third = third;
		}

		public bool IsLessThan(int first, int second, int third)
		{
			if (First < first) return true;
			if (Second < second) return true;
			if (Third < third) return true;
			return false;
		}

		public override string ToString()
		{
			return string.Format("{0}.{1}.{2}", First, Second, Third);
		}
	}
}
