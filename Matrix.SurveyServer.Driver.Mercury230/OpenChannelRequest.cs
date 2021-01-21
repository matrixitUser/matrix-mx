using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	/// <summary>
	/// запрос на открытие канала
	/// Запрос на открытие канала связи предназначен для разрешения доступа к дан-
	/// ным с указанием уровня доступа. В счетчике реализован двухуровневый доступ к данным:
	/// первый (низший) - уровень потребителя, и второй (высший) - уровень хозяина.
	/// </summary>
	class OpenChannelRequest : Base
	{
		public OpenChannelRequest(byte networkAddress, Level level, string password)
			: base(networkAddress, 0x01)
		{
			Data.Add((byte)level);

			///поле пароля имеет размер 6 байт, и в качестве символов пароля допускаются любые
			///символы клавиатуры компьютера с учетом регистра.
			if (string.IsNullOrEmpty(password) || password.Length < 6)
			{
				for (int i = 0; i < 6; i++)
				{
					Data.Add(0x01);
				}
			}
			else
			{
				for (int i = 0; i < 6; i++)
				{
					byte passByte = 1;
					byte.TryParse(password[i].ToString(), out passByte);
					Data.Add(passByte);
				}
			}
		}
	}

	/// <summary>
	/// уровень доступа
	/// </summary>
	enum Level : byte
	{
		/// <summary>
		/// низший - уровень потребителя
		/// </summary>
		Slave = 1,
		/// <summary>
		/// высший - уровень хозяина
		/// </summary>
		Master = 2
	}
}
