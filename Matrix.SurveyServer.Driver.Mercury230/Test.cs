using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
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

        byte[] MakeTestRequest()
        {
            return MakeBaseRequest(0x00, new List<byte>());
        }

        byte[] MakeOpenChannelRequest(Level level, string password)
        {
            var Data = new List<byte>();
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

            return MakeBaseRequest(0x01, Data);
        }

        dynamic ParseTestResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            answer.success = answer.Body[0] == 0x00 || answer.Body[0] == 0x80;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            return answer;
        }
    }
}
