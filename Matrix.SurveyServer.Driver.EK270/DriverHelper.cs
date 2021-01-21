using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	static class DriverHelper
	{
		private static readonly string DateTimeFormat = "yyyy-MM-dd, HH:mm:ss";
		/// <summary>
		/// Идентификационное сообщение - самое превое, с него начинается общение с контроллером
		/// </summary>
		/// <returns></returns>
		public static byte[] GetIdentificationMessage()
		{
			byte[] initialPart = new byte[]{
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00,};
			var request = new List<byte>(initialPart);

			//request.AddRange(new byte[]{0x2F, 0x3F, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 
			//        0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x21, 0x0D, 0x0A});

			request.AddRange(Encoding.ASCII.GetBytes("/?000000000000!"));
			request.Add(AsciiCmdSymbol.CR);
			request.Add(AsciiCmdSymbol.LF);
			return request.ToArray();
		}
		/// <summary>
		/// Выбор опций
		/// </summary>
		/// <param name="speed"></param>
		/// <returns></returns>
		public static byte[] GetChoiceOptionMessage(byte speed)
		{
			return new byte[]{
                AsciiCmdSymbol.ACK,//симол ACK - положительный ответ контроллеру
                Convert('0'),// 0 - нормальная процедура протокола
                speed,// работаем на той скорости, которую предложил контроллер
                Convert('1'), //режим программирования
                AsciiCmdSymbol.CR,//конец сообщения
                AsciiCmdSymbol.LF//конец сообщения
            };
		}
		/// <summary>
		/// Код доступа потребителя
		/// </summary>
		/// <returns></returns>
		public static byte[] GetAccessCodeMessage(string password = "0")
		{
			return GetAccessMessage(1, password);
		}
		/// <summary>
		/// Закрыть доступ потребителю
		/// </summary>
		/// <returns></returns>
		public static byte[] GetCloseAccessMessage(string password = "0")
		{
			return GetAccessMessage(0, password);
		}
		/// <summary>
		/// Открыть или закрыть доступ потребителю
		/// </summary>
		/// <param name="code">0 - закрыть, 1 - открыть</param>
		/// <returns></returns>
		public static byte[] GetAccessMessage(int code, string password)
		{
			if (string.IsNullOrEmpty(password))
				password = "00000000";
			else if (password.Length != 8)
				password = "00000000";

			if (code != 1 && code != 0)
				return null;
			var request = new List<byte>();
			request.Add(AsciiCmdSymbol.SOH);
			request.AddRange(Convert("W1"));
			request.Add(AsciiCmdSymbol.STX);
			var x = string.Format("4:17{0}.0({1})", code, password);
			request.AddRange(Convert(x));
			request.Add(AsciiCmdSymbol.ETX);
			request.Add(CalculateBcc(request.ToArray()));
			return request.ToArray();
		}

		//public static  byte[] GetPassword(string password)
		//{
		//    //<SOH>P0<STX>(1234567)<ETX>P
		//    var request = new List<byte>();
		//    request.Add(AsciiCmdSymbol.SOH);
		//    request.AddRange(Convert("P0"));
		//    request.Add(AsciiCmdSymbol.STX);
		//    request.AddRange(Convert(string.Format("({0})", password)));
		//    request.Add(AsciiCmdSymbol.ETX);
		//    request.Add(CalculateBcc(request.ToArray()));
		//    return request.ToArray();
		//}

		public static byte[] GetEventsArchiveMessage(DateTime? start, DateTime? end)
		{
			return GetArchiveMessage(start, end, 4);
		}

		public static byte[] GetMonthly1ArchiveMessage(DateTime? start, DateTime? end)
		{
			return GetArchiveMessage(start, end, 1);
		}
		public static byte[] GetMonthly2ArchiveMessage(DateTime? start, DateTime? end)
		{
			return GetArchiveMessage(start, end, 2);
		}
		public static byte[] GetIntervalArchiveMessage(DateTime? start, DateTime? end)
		{
			return GetArchiveMessage(start, end, 3);
		}

		public static byte[] GetMonthly1ArchiveHeaderMessage()
		{
			return GetArchiveHeaderMessage(1);
		}
		public static byte[] GetMonthly2ArchiveHeaderMessage()
		{
			return GetArchiveHeaderMessage(2);
		}
		public static byte[] GetIntervalArchiveHeaderMessage()
		{
			return GetArchiveHeaderMessage(3);
		}

		public static byte[] GetMonthly1ArchiveUnitMeasureMessage()
		{
			return GetArchiveUnitMeasureMessage(1);
		}
		public static byte[] GetMonthly2ArchiveUnitMeasureMessage()
		{
			return GetArchiveUnitMeasureMessage(2);
		}
		public static byte[] GetIntervalArchiveUnitMeasureMessage()
		{
			return GetArchiveUnitMeasureMessage(3);
		}
		public static byte[] GetSingleValueMessage(string address)
		{
			var request = new List<byte> { AsciiCmdSymbol.SOH };
			request.AddRange(Convert("R1"));
			request.Add(AsciiCmdSymbol.STX);
			request.AddRange(Convert(string.Format("{0}(1)", address)));
			request.Add(AsciiCmdSymbol.ETX);
			request.Add(CalculateBcc(request.ToArray()));
			return request.ToArray();
		}
		/// <summary>
		/// Получить архив
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="code">Код архива: 1 - ежемесячный (счетчики), 2 - ежемесячный (измерения), 3 - интервальный</param>
		/// <returns></returns>
		public static byte[] GetArchiveMessage(DateTime? start, DateTime? end, int code)
		{
			if (code < 1 || code > 4)
				return null;
			var request = new List<byte>();
			request.Add(AsciiCmdSymbol.SOH);
			request.AddRange(Convert("R3"));
			request.Add(AsciiCmdSymbol.STX);
			request.AddRange(Convert(string.Format("{2}:V.0(3;{0};{1};1)",
				start.HasValue ? start.Value.ToString(DateTimeFormat) : string.Empty,
				end.HasValue ? end.Value.ToString(DateTimeFormat) : string.Empty, code)));
			request.Add(AsciiCmdSymbol.ETX);
			request.Add(CalculateBcc(request.ToArray()));
			return request.ToArray();
		}
		public static byte[] GetArchiveHeaderMessage(int code)
		{
			var request = new List<byte>();
			request.Add(AsciiCmdSymbol.SOH);
			request.AddRange(Convert("R1"));
			request.Add(AsciiCmdSymbol.STX);
			request.AddRange(Convert(string.Format("{0}:V.2(1)", code)));
			request.Add(AsciiCmdSymbol.ETX);
			request.Add(CalculateBcc(request.ToArray()));
			return request.ToArray();
		}
		public static byte[] GetArchiveUnitMeasureMessage(int code)
		{
			if (code != 1 && code != 2 && code != 3)
				return null;
			var request = new List<byte>();
			request.Add(AsciiCmdSymbol.SOH);
			request.AddRange(Convert("R1"));
			request.Add(AsciiCmdSymbol.STX);
			request.AddRange(Convert(string.Format("{0}:V.3(1)", code)));
			request.Add(AsciiCmdSymbol.ETX);
			request.Add(CalculateBcc(request.ToArray()));
			return request.ToArray();
		}

		public static byte Convert(char c)
		{
			return Encoding.ASCII.GetBytes(new char[] { c })[0];
		}
		public static byte[] Convert(string s)
		{
			return Encoding.ASCII.GetBytes(s);
		}
		/// <summary>
		/// Проверить, правильно ли подсчитано bcc в сообщении
		/// </summary>
		/// <param name="buffer">Весь буфер, в том числе с первым неучитываемом символом</param>
		/// <returns></returns>
		public static bool CheckBcc(byte[] buffer)
		{
			if (buffer == null || buffer.Length < 3) return false;
			//первым симолом должен идти <STX> или <SOH> - их учитывать не надо
			char bcc = Encoding.ASCII.GetChars(new byte[] { buffer[1] })[0];

			if (buffer.Length > 2)
				for (int i = 2; i < buffer.Length - 1; i++)
				{
					bcc ^= Encoding.ASCII.GetChars(new byte[] { buffer[i] })[0];
				}

			return bcc == Encoding.ASCII.GetChars(new byte[] { buffer[buffer.Length - 1] })[0];
		}
		public static byte CalculateBcc(byte[] buffer)
		{
			if (buffer == null || buffer.Length < 2) return default(byte);

			char bcc = Encoding.ASCII.GetChars(new byte[] { buffer[1] })[0];

			if (buffer.Length > 2)
				for (int i = 2; i < buffer.Length; i++)
				{
					bcc ^= Encoding.ASCII.GetChars(new byte[] { buffer[i] })[0];
				}

			return Encoding.ASCII.GetBytes(new char[] { bcc })[0];
		}


	}
	/// <summary>
	/// Управляющие симолы ASCII
	/// </summary>
	class AsciiCmdSymbol
	{
		/// <summary>
		/// Симол начала заголовка
		/// </summary>
		public static readonly byte SOH = 0x01;
		/// <summary>
		///  Символ начала структуры в блоке с проверочным символом.
		///  Этот символ не требуется если за ним не следуют никакие данные
		/// </summary>
		public static readonly byte STX = 0x02;
		/// <summary>
		/// Символ конца блока 
		/// </summary>
		public static readonly byte ETX = 0x03;
		/// <summary>
		/// Символ конца в частичном блоке
		/// </summary>
		public static readonly byte EOT = 0x04;
		/// <summary>
		/// Символ подтверждения
		/// </summary>
		public static readonly byte ACK = 0x06;
		/// <summary>
		/// Возврат каретки
		/// </summary>
		public static readonly byte CR = 0x0d;
		/// <summary>
		/// Перевод строки
		/// </summary>
		public static readonly byte LF = 0x0a;

		public static readonly byte[] MessageEnd = new byte[] { CR, LF };
	}
}
