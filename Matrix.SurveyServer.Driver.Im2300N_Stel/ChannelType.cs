using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
	enum ChannelType
	{
		S = 0x00,
		M = 0x50,
		T = 0x60,
		I = 0x20,
		DI = 0x70,
		R = 0x10,
		dP = 0x70,
		P = 0x30,
		F = 0x40,
		U = 0xA0,
		B = 0x90,
		W = 0x80
	}
}
