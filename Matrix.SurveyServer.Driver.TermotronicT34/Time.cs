using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    /// <summary>
    /// запрос на чтение массивов времен
    /// При чтении текущего времени поля «Параметр» и «№ записи» объединены в 1 байт.
    /// Глубина журнала событий составляет 10 записей. Нумерация номера записи начина-
    /// ется с нуля. Это означает, что записи последовательно заносятся в массив журнала событий
    /// с нарастанием номера записи и после 9 записи прибором будет произведена запись по адре-
    /// су нулевой записи.
    /// Начиная с версии ПО 2.1.0, введен массив контроля за временем вскрытия/закрытия
    /// прибора
    /// Начиная с версии ПО 2.1.0, введена функция чтения последней сделанной записи для
    /// каждого из массивов журнала событий. Данный запрос осуществляется с значением номера
    /// записи, равным FFh. К 8 байтам стандартного ответа добавляется 9-й байт – номер записи.
    /// </summary>
    public partial class Driver
    {
        byte[] MakeTimeRequest(byte parameter, byte record)
        {
            var Data = new List<byte>();
            Data.Add(parameter);

            if (parameter != 0x00)
            {
                Data.Add(record);
            }

            return MakeBaseRequest(0x04, Data);
        }

        byte[] MakeTimeCorrectionRequest(int hour, int min, int sec)
        {
            return MakeBaseRequest(0x03, new List<byte> { 0x0D, Helper.ToBCD1((byte)(sec % 60)), Helper.ToBCD1((byte)(min % 60)), Helper.ToBCD1((byte)(hour % 24))});
        }


        dynamic ParseTimeResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            if(answer.Body.Length < 8)
            {
                answer.error = "Слишком короткий ответ";
                answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                return answer;
            }

            dynamic time = new ExpandoObject();
            time.success = true;
            time.error = string.Empty;
            time.errorcode = DeviceError.NO_ERROR;

            var second = Helper.FromBCD(answer.Body[0]);
            var minute = Helper.FromBCD(answer.Body[1]);
            var hour = Helper.FromBCD(answer.Body[2]);
            var unknown = Helper.FromBCD(answer.Body[3]);
            var day = Helper.FromBCD(answer.Body[4]);
            var month = Helper.FromBCD(answer.Body[5]);
            var year = Helper.FromBCD(answer.Body[6]);
            time.IsWinter = (answer.Body[7] == 1);
            time.date = new DateTime(2000 + year, month, day, hour, minute, second);

            return time;
        }
    }
}
