using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private DateTime ParseDate(string stringValue)
        {
            return DateTime.ParseExact(stringValue, "yyyy-MM-dd,HH:mm:ss", null);
        }

        private double ParseFloat(string stringValue)
        {
            double f = 0.0;
            double.TryParse(stringValue.Replace('.', ','), out f);
            //log(string.Format("парсинг {0}=>{1}", stringValue, f));
            //  log(string.Format("float {0}; value {1}", stringValue, f));
            return f;
        }


        private int ParseCheckError(byte[] bytes)
        {
            if (bytes.Count() < 7) return 10000;
            var str = Encoding.ASCII.GetString(bytes, 1, bytes.Length - 1);
            var cells = str.Replace("(", "").Replace(")", "\n").Split('\n');
            if (cells[0].StartsWith("#"))
            {
                int errcode;
                if (!int.TryParse(cells[0].Substring(1), out errcode)) return -1;
                return errcode;
            }
            return 0;
        }

        private string GetErrorText(int errcode)
        {
            string s = "Неизвестная ошибка";
            switch (errcode)
            {
                case 0: s = ""; break;
                case 1: s = "Неверный адрес"; break;
                case 2: s = "Неверный тип кода в адресе"; break;
                case 3: s = "Неверный номер объекта в адресе"; break;
                case 4: s = "Неверный атрибут в адресе (Первый из четырех символов команды или перед открывающейся скобкой)"; break;
                case 5: s = "Отсутствует атрибут для данного параметра"; break;
                case 6: s = "Значение вне допустимых границ"; break;
                case 9: s = "Значение не может быть записано (например, параметр является измерением или константой)"; break;
                case 13: s = "Некорректный ввод"; break;
                case 17: s = "Неверная комбинация (числовой код для открытия замка)"; break;
                case 18: s = "Невозможно прочитать значение, потому что соответствующий замок не открыт"; break;
                case 19: s = "Невозможно записать значение, потому что соответствующий замок не открыт"; break;
                case 20: s = "Действие не разрешено"; break;
                case 100: s = "Неверный номер архива"; break;
                case 101: s = "Запрашиваемая запись не найдена"; break;
                case 103: s = "Архив пуст"; break;
                case 200: s = "Недопустимый формат ввода (синтаксическая ошибка)"; break;
                case 1000: s = "Короткий ответ"; break;
                case 1001: s = "Короткий ответ"; break;
            }
            return s;
        }
    }
}
