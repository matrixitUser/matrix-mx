using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на запись типа значений
	/// см. док. п. 4.3 стр. 14
	/// </summary>
	public class RequestWriteValueType : RequestWrite
	{
		private ValueType archiveType;
		public RequestWriteValueType(byte networkAddress, ValueType archiveType)
			: base(networkAddress, 0x3ffd, 0x0000, new byte[] 
			{ 
				Helper.GetLowByte((short)archiveType),
				Helper.GetHighByte((short)archiveType)
			})
		{
			this.archiveType = archiveType;
		}

		public override string ToString()
		{
			return string.Format("запись типа значений: {0}", archiveType);
		}
	}

	/// <summary>
	/// тип значений
	/// см. док. п. 4.3 стр. 14
	/// </summary>
	public enum ValueType : short
	{
		/// <summary>
		/// часовой архив
		/// </summary>
		Hour = 0x0000,
		/// <summary>
		/// суточный архив
		/// </summary>
		Day = 0x0001,
		/// <summary>
		/// месячный архив
		/// </summary>
		Month = 0x0002,
		/// <summary>
		/// итоговый архив
		/// </summary>
		Total = 0x0003,
		/// <summary>
		/// текущие значения
		/// </summary>
		Current = 0x0004,
		/// <summary>
		/// итоговые текущие
		/// </summary>
		TotalCurrent = 0x0005,
		/// <summary>
		/// свойства
		/// </summary>
		Properties = 0x0006
	}

}
