using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761.Protocol
{
	enum FunctionCode
	{
		/// <summary>
		/// временной срез архива
		/// </summary>
		ArchiveTimeSection = 0x18,

		/// <summary>
		/// структура архива
		/// </summary>
		ArchiveStructure = 0x19,

		/// <summary>
		/// чтение параметра
		/// </summary>
		ReadParameter = 0x1D,

		ReadArrayItem = 0x0C
	}
}
