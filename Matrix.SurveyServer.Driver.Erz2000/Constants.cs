using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private string GetPressUnitByCode(int code)
        {
            switch (code)
            {
                case 0: return "бар";
                default: return "не реализован, код " + code;
            }
        }

        private string GetTempUnitByCode(int code)
        {
            switch (code)
            {
                case 0: return "°С";
                default: return "не реализован, код " + code;
            }
        }

        private string GetMethUnitByCode(int code)
        {
            switch (code)
            {
                case 3: return "AGA NX 19 L";
                default: return "не реализован, код " + code;
            }
        }

        private string GetSensorByCode(int code)
        {
            switch (code)
            {
                case 0x0e: return "USZ";
                default: return "не реализован, код " + code;
            }
        }

        private string GetDevtypeByCode(int code)
        {
            switch (code)
            {
                case 3: return "Ультразвук.счетчик";
                default: return "не реализован, код " + code;
            }
        }

        private dynamic GetConstants(byte na, DateTime date)
        {
            dynamic constants = new ExpandoObject();
            constants.success = true;
            constants.records = new List<dynamic>();

            var regs = ParseModbus(Send(MakeModbus3Request(na, 0, 50)));
            if (regs.success)
            {
                constants.records.Add(MakeConstRecord("Единица изм. давления", GetPressUnitByCode(ToInt32(regs.body, 1 + 0 * 4)), date));
                constants.records.Add(MakeConstRecord("Давление по-умолчанию, бар", ToSingle(regs.body, 1 + 1 * 4), date));
                constants.records.Add(MakeConstRecord("Нижний предел тревоги изм. давления, бар", ToSingle(regs.body, 1 + 2 * 4), date));
                constants.records.Add(MakeConstRecord("Верхний предел тревоги изм. давления, бар", ToSingle(regs.body, 1 + 3 * 4), date));
                constants.records.Add(MakeConstRecord("Выбор входной величины датчика давления, бар", "токовый канал " + ToInt32(regs.body, 1 + 4 * 4), date));

                constants.records.Add(MakeConstRecord("Единица изм. температуры", GetTempUnitByCode(ToInt32(regs.body, 1 + 7 * 4)), date));
                constants.records.Add(MakeConstRecord("Температура по-умолчанию, °C", ToSingle(regs.body, 1 + 8 * 4), date));
                constants.records.Add(MakeConstRecord("Нижний предел тревоги изм. температуры, °C", ToSingle(regs.body, 1 + 9 * 4), date));
                constants.records.Add(MakeConstRecord("Верхний предел тревоги изм. температуры, °C", ToSingle(regs.body, 1 + 10 * 4), date));
                constants.records.Add(MakeConstRecord("Выбор входной величины датчика температуры, °C", "токовый канал " + ToInt32(regs.body, 1 + 11 * 4), date));

                constants.records.Add(MakeConstRecord("Метод расчета коэффициента К", GetMethUnitByCode(ToInt32(regs.body, 1 + 14 * 4)), date));
                constants.records.Add(MakeConstRecord("Номинальный диаметр, мм", ToSingle(regs.body, 1 + 15 * 4), date));
                constants.records.Add(MakeConstRecord("Минимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 16 * 4), date));
                constants.records.Add(MakeConstRecord("Максимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 17 * 4), date));
                constants.records.Add(MakeConstRecord("Граница чувствительности, м³/ч", ToSingle(regs.body, 1 + 18 * 4), date));
                constants.records.Add(MakeConstRecord("Выбор датчика объема", GetSensorByCode(ToInt32(regs.body, 1 + 19 * 4)), date));

                constants.records.Add(MakeConstRecord("Тип счетчика", GetDevtypeByCode(ToInt32(regs.body, 1 + 22 * 4)), date));
                constants.records.Add(MakeConstRecord("Зональное время и правило экономии времени светового дня", "ETC/GMT-5", date));

                constants.contractHour = ToInt32(regs.body, 1 + 24 * 4);
                constants.records.Add(MakeConstRecord("Час подсчета", constants.contractHour, date));                
            }

            //regs = ParseModbus(Send(MakeModbus3Request(na, 14, 10)));
            //if (regs.success)
            //{
            //    constants.records.Add(MakeConstRecord("Единица изм. температуры", GetTempUnitByCode(ToInt32(regs.body, 1 + 0 * 4)), date));
            //    constants.records.Add(MakeConstRecord("Температура по-умолчанию, °C", ToSingle(regs.body, 1 + 1 * 4), date));
            //    constants.records.Add(MakeConstRecord("Нижний предел тревоги изм. температуры, °C", ToSingle(regs.body, 1 + 2 * 4), date));
            //    constants.records.Add(MakeConstRecord("Верхний предел тревоги изм. температуры, °C", ToSingle(regs.body, 1 + 3 * 4), date));
            //    constants.records.Add(MakeConstRecord("Выбор входной величины датчика температуры, °C", "токовый канал " + ToInt32(regs.body, 1 + 4 * 4), date));
            //}

            //regs = ParseModbus(Send(MakeModbus3Request(na, 28, 8)));
            //if (regs.success)
            //{
            //    constants.records.Add(MakeConstRecord("Метод расчета коэффициента К", GetMethUnitByCode(ToInt32(regs.body, 1 + 0 * 4)), date));
            //    constants.records.Add(MakeConstRecord("Номинальный диаметр, мм", ToSingle(regs.body, 1 + 1 * 4), date));
            //    constants.records.Add(MakeConstRecord("Минимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 2 * 4), date));
            //}

            //regs = ParseModbus(Send(MakeModbus3Request(na, 34, 8)));
            //if (regs.success)
            //{
            //    constants.records.Add(MakeConstRecord("Максимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 0 * 4), date));
            //    constants.records.Add(MakeConstRecord("Граница чувствительности, м³/ч", ToSingle(regs.body, 1 + 1 * 4), date));
            //    constants.records.Add(MakeConstRecord("Выбор датчика объема", GetSensorByCode(ToInt32(regs.body, 1 + 2 * 4)), date));
            //}

            //regs = ParseModbus(Send(MakeModbus3Request(na, 44, 8)));
            //if (regs.success)
            //{
            //    constants.records.Add(MakeConstRecord("Тип счетчика", GetDevtypeByCode(ToInt32(regs.body, 1 + 0 * 4)), date));
            //    constants.records.Add(MakeConstRecord("Зональное время и правило экономии времени светового дня", "ETC/GMT-5", date));
            //    constants.records.Add(MakeConstRecord("Час подсчета", ToInt32(regs.body, 1 + 2 * 4), date));
            //}

            //306 плотн 1210 выкинуть
            var v = ParseFloat(Send(MakeModbus3Request(na, 1206, 2)));
            if (v.success)
            {
                constants.records.Add(MakeConstRecord("Плотность", string.Format("{0}, кг/м³", v.value), date));
            }

            regs = ParseModbus(Send(MakeModbus3Request(na, 400, 12)));
            if (regs.success)
            {
                constants.records.Add(MakeConstRecord("Метод расчета коэффициента К", GetMethUnitByCode(ToInt32(regs.body, 1 + 0 * 4)), date));
                constants.records.Add(MakeConstRecord("Номинальный диаметр, мм", ToSingle(regs.body, 1 + 1 * 4), date));
                constants.records.Add(MakeConstRecord("Минимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 2 * 4), date));
                constants.records.Add(MakeConstRecord("Максимальный рабочий расход, м³/ч", ToSingle(regs.body, 1 + 3 * 4), date));
                constants.records.Add(MakeConstRecord("Граница чувствительности, м³/ч", ToSingle(regs.body, 1 + 4 * 4), date));
                constants.records.Add(MakeConstRecord("Выбор датчика объема", GetSensorByCode(ToInt32(regs.body, 1 + 5 * 4)), date));
            }

            v = ParseFloat(Send(MakeModbus3Request(na, 400, 2)));
            if (v.success)
            {
                constants.records.Add(MakeConstRecord("CO₂", string.Format("{0}", v.value), date));
            }

            //v = ParseFloat(Send(MakeModbus3Request(na, 402, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Водород", string.Format("{0}", v.value), date));
            //}

            v = ParseFloat(Send(MakeModbus3Request(na, 404, 2)));
            if (v.success)
            {
                constants.records.Add(MakeConstRecord("Азот", string.Format("{0}", v.value), date));
            }

            //v = ParseFloat(Send(MakeModbus3Request(na, 406, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Метан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 408, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Этан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 410, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Пропан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 412, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("N-бутан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 414, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("I-бутан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 416, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("N-пентан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 418, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("I-пентан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 420, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Нео-пентан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 422, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Гексан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 424, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Гептан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 426, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Октан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 428, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Нонан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 430, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Декан", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 432, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Сероводород", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 434, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Вода", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 436, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Гелий", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 438, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Кислород", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 440, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Окись углерода", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 442, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Этилен", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 444, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Пропен", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 446, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Аргон", string.Format("{0}", v.value), date));
            //}

            //v = ParseFloat(Send(MakeModbus3Request(na, 446, 2)));
            //if (v.success)
            //{
            //    constants.records.Add(MakeConstRecord("Абс. P (режим работы)", string.Format("{0}", v.value), date));
            //}

            return constants;
        }
    }
}
