using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetBODFlash3(byte na, short password, DateTime date)
        {
            var answer = ParseModbusResponse(SendWithCrc(Make16Request(na, RVS, new short[] { 0x00E2 })));
            if (!answer.success) return answer;

            return ParseBODFlash3Response(SendWithCrc(Make3Request(na, 0x0000, 55)), date);
        }

        private dynamic ParseBODFlash3Response(byte[] bytes, DateTime date)
        {
            var bod = ParseModbusResponse(bytes);
            if (!bod.success) return bod;
            byte[] swapBytes = new byte[bod.body.Length - 1];
            // первый байт - длина массива

            for (int i = 1; i < bod.body.Length - 1; i += 2)
            {
                swapBytes[i - 1] = bod.body[i + 1];
                swapBytes[i] = bod.body[i];
            }
            if ((swapBytes.Length - 1) % 2 == 1) swapBytes[bod.body.Length - 2] = bod.body[bod.body.Length - 1];

            bod.records = new List<dynamic>();
            bod.contractHour = BinDecToInt(swapBytes[37]);
            bod.version = BitConverter.ToInt16(swapBytes, 0);

            bod.records.Add(MakeConstantRecord("Версия прошивки регистратора", bod.version, date));
            bod.records.Add(MakeConstantRecord("Адрес регистратора в сети ModBus", swapBytes[2], date));
            bod.records.Add(MakeConstantRecord("Адрес ПП в сети ModBus", swapBytes[3], date));
            bod.records.Add(MakeConstantRecord("Порт 1. Скорость обмена в сети ModBus", GetSpeed(swapBytes[4]), date));
            bod.records.Add(MakeConstantRecord("Скорость обмена ПП в сети ModBus", GetSpeed(swapBytes[5]), date));

            byte reg = swapBytes[8];
            bool auto = ((byte)(reg & 0x04) == 0x04);
            bod.records.Add(MakeConstantRecord("Автоматический перевод зима/лето", auto ? "автоматический" : "ручной", date));

            bod.records.Add(MakeConstantRecord("Договорная температура x100, °K", BitConverter.ToUInt16(swapBytes, 9), date));
            bod.records.Add(MakeConstantRecord("Договорное давление, кПа", BitConverter.ToUInt16(swapBytes, 11), date));

            bod.records.Add(MakeConstantRecord("Договорной расход при температуре меньшей граничной, нм³/час", BitConverter.ToUInt32(swapBytes, 13), date));
            bod.records.Add(MakeConstantRecord("Договорной расход при температуре большей граничной, нм³/час", BitConverter.ToUInt32(swapBytes, 17), date));            
            bod.records.Add(MakeConstantRecord("Граничная температура, °K", BitConverter.ToUInt16(swapBytes, 21), date));

            bod.records.Add(MakeConstantRecord("Порт 2. Скорость обмена в сети ModBus", GetSpeed(swapBytes[28]), date));
            bod.records.Add(MakeConstantRecord("Контрактный час, ч", bod.contractHour, date));
            bod.records.Add(MakeConstantRecord("Начало месяца", BinDecToInt(swapBytes[38]), date));
            //  bod.records.Add(MakeConstantRecord("Час перевода зима/лето", BinDecToInt(swapBytes[49]), date));
            bod.records.Add(MakeConstantRecord("Значение коррекции часов, секунд/сутки", swapBytes[50], date));
            return bod;
        }

        private int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }

        private bool Transfer(byte flg)
        {
            return true;
        }
    }
}
