using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetBODFlash7(byte na, short password, DateTime date)
        {
            var answer = ParseModbusResponse(SendWithCrc(Make16Request(na, RVS, new short[] { 0x0054 })));
            if (!answer.success) return answer;

            return ParseBODFlash7Response(SendWithCrc(Make3Request(na, 0x0000, 0x0081 / 2)), date);
        }

        private dynamic ParseBODFlash7Response(byte[] bytes, DateTime date)
        {
            var bod = ParseModbusResponse(bytes);
            if (!bod.success) return bod;
            var swapBytes = new byte[bod.body.Length];

            for (int i = 0; i < bod.body.Length - 2; i += 2)
            {
                swapBytes[i] = bod.body[i + 1 + 1];
                swapBytes[i + 1] = bod.body[i + 1];
            }

            bod.records = new List<dynamic>();
            bod.contractHour = swapBytes[15];
            bod.version = BitConverter.ToInt16(swapBytes, 0);

            bod.records.Add(MakeConstantRecord("Версия прошивки регистратора", bod.version, date));
            bod.records.Add(MakeConstantRecord("Адрес регистратора в сети ModBus", swapBytes[2], date));
            bod.records.Add(MakeConstantRecord("Порт 1. Скорость обмена в сети ModBus", GetSpeed(swapBytes[7]), date));

            short reg = BitConverter.ToInt16(swapBytes, 11);
            bool auto = ((short)(reg & 0x0004) == 0x0004);
            bod.records.Add(MakeConstantRecord("Автоматический перевод зима/лето", auto ? "автоматический" : "ручной", date));

            // bod.records.Add(MakeConstantRecord("Порт 2. Скорость обмена в сети ModBus", GetSpeed(swapBytes[129]), date));
            bod.records.Add(MakeConstantRecord("Контрактный час, ч", bod.contractHour, date));
            bod.records.Add(MakeConstantRecord("Начало месяца", swapBytes[16], date));
            // bod.records.Add(MakeConstantRecord("Час перевода зима/лето", swapBytes[79], date));
            bod.records.Add(MakeConstantRecord("Значение коррекции часов, секунд/сутки", swapBytes[80], date));

            bod.records.Add(MakeConstantRecord("Договорный расход ПП1, н.м³/час", BitConverter.ToInt32(swapBytes, 17), date));
            bod.records.Add(MakeConstantRecord("Договорный расход ПП2", BitConverter.ToInt32(swapBytes, 21), date));
            bod.records.Add(MakeConstantRecord("Договорный расход ПП3", BitConverter.ToInt32(swapBytes, 25), date));
            bod.records.Add(MakeConstantRecord("Договорный расход ПП4", BitConverter.ToInt32(swapBytes, 29), date));

            bod.records.Add(MakeConstantRecord("Производственно-технологические н.м³ ПП1", BitConverter.ToInt32(swapBytes, 33), date));
            bod.records.Add(MakeConstantRecord("Производственно-технологические н.м³ ПП2", BitConverter.ToInt32(swapBytes, 37), date));
            bod.records.Add(MakeConstantRecord("Производственно-технологические н.м³ ПП3", BitConverter.ToInt32(swapBytes, 41), date));
            bod.records.Add(MakeConstantRecord("Производственно-технологические н.м³ ПП4", BitConverter.ToInt32(swapBytes, 45), date));

            return bod;
        }

        private int GetSpeed(byte code)
        {
            switch (code)
            {
                case 0: return 2400;
                case 1: return 4800;
                case 2: return 9600;
                case 3: return 14400;
                default: return 19200;
            }
        }
    }
}
