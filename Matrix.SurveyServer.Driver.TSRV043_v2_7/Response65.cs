using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TSRV043
{
	/// <summary>
	/// 
	/// </summary>
	class Response65 : Response
	{
		public List<Data> Data { get; private set; }

		public int Channel { get; private set; }

        public string Text { get; private set; }

        public Response65(byte[] data, short index) : base(data)
        {
            Text = "";
            Data = new List<Common.Data>();
            var length = data[2];
            int start = 3;

            //Метка времени с u32 Время окончания интервала - 1 с, от 00:00:00 01.01.1970
            var seconds = Helper.ToUInt32(data, start + 0);
            //если данные нулевые, игнорим их
            if (seconds == 0) return;
            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);
            Text += string.Format("{0:dd.MM.yy HH:mm} ", date);

            //4 Индекс архивной записи
            var archiveIndex = Helper.ToUInt16(data, start + 4);

            //6 Итоговое время наработки с u32         1
            double timeWork = Helper.ToUInt32(data, start + 6)/60.0;
            Data.Add(new Data(string.Format("Итоговое время наработки"), MeasuringUnitType.min, date, timeWork));

            //10 Итоговый объём 1 л u32
            double  volume1 = Helper.ToUInt32(data, start + 10)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 1"),MeasuringUnitType.m3, date, volume1));
            //14 Итоговый объём 2/2п л u32
            double volume2 = Helper.ToUInt32(data, start + 14)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 2/2п"),MeasuringUnitType.m3, date, volume2));
            //18 Итоговый объём 3 л u32
            double volume3 = Helper.ToUInt32(data, start + 18)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 3"),MeasuringUnitType.m3, date, volume3));
            //22 Итоговый объём 4/4п л u32
            double volume4 = Helper.ToUInt32(data, start + 22)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 4/4п"),MeasuringUnitType.m3, date, volume4));
            //26 Итоговый объём 5/2о л u32
            double volume5 = Helper.ToUInt32(data, start + 26)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 5/2о"),MeasuringUnitType.m3, date, volume5));
            //30 Итоговый объём 6 / 4о л u32
            double volume6 = Helper.ToUInt32(data, start + 30)/1000.0;
            Data.Add(new Data(string.Format("Итоговый объём 6"),MeasuringUnitType.m3, date, volume6));

            //34 Итоговая масса 1 кг u32 
            double massa1 = Helper.ToUInt32(data, start + 34)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 1"),MeasuringUnitType.tonn, date, massa1));
            //38 Итоговая масса 2/2п кг u32 
            double massa2 = Helper.ToUInt32(data, start + 38)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 2/2п"),MeasuringUnitType.tonn, date, massa2));
            //42 Итоговая масса 3 кг u32 
            double massa3 = Helper.ToUInt32(data, start + 42) / 1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 3"),MeasuringUnitType.tonn, date, massa3));
            //46 Итоговая масса 4/4п кг u32 
            double massa4 = Helper.ToUInt32(data, start + 46) / 1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 4/4п"),MeasuringUnitType.tonn, date, massa4));
            //50 Итоговая масса 5/2о кг u32 
            double massa5 = Helper.ToUInt32(data, start + 50) / 1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 5/2о"),MeasuringUnitType.tonn, date, massa5));
            //54 Итоговая масса 6/4о кг u32 
            double massa6 = Helper.ToUInt32(data, start + 54) / 1000.0;
            Data.Add(new Data(string.Format("Итоговая масса 6/4о"),MeasuringUnitType.tonn, date, massa6));

            //58 Итоговая масса по ТС1 кг u32 
            double massa1hs = Helper.ToInt32(data, start + 58)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса по ТС1"),MeasuringUnitType.tonn, date, massa1hs));
            //62 Итоговая масса по ТС2 кг u32 
            double massa2hs = Helper.ToInt32(data, start + 62)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса по ТС2"),MeasuringUnitType.tonn, date, massa2hs));
            //66 Итоговая масса по ТС3 кг u32 
            double massa3hs = Helper.ToInt32(data, start + 66)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса по ТС3"),MeasuringUnitType.tonn, date, massa3hs));
            //70 Итоговая масса по ТС4 кг u32 
            double massa4hs = Helper.ToInt32(data, start + 70)/1000.0;
            Data.Add(new Data(string.Format("Итоговая масса по ТС4"),MeasuringUnitType.tonn, date, massa4hs));

            //74 Итоговая ТЭ по ТС1 МДж u32  пишем в гКал    (1 MДж ≈ 238,846 ГКал)
            double heat1hs = Helper.ToInt32(data, start + 74) /4186.8;
            Data.Add(new Data(string.Format("Итоговая ТЭ по ТС1"),MeasuringUnitType.Gkal, date, heat1hs));
            //78 Итоговая ТЭ по ТС2 МДж u32 
            double heat2hs = Helper.ToInt32(data, start + 78) / 4186.8;
            Data.Add(new Data(string.Format("Итоговая ТЭ по ТС2"),MeasuringUnitType.Gkal, date, heat2hs));
            //82 Итоговая ТЭ по ТС1 МДж u32 
            double heat3hs = Helper.ToInt32(data, start + 82) / 4186.8;
            Data.Add(new Data(string.Format("Итоговая ТЭ по ТС3"),MeasuringUnitType.Gkal, date, heat3hs));
            //86 Итоговая ТЭ по ТС1 МДж u32 
            double heat4hs = Helper.ToInt32(data, start + 86) / 4186.86;
            Data.Add(new Data(string.Format("Итоговая ТЭ по ТС4"),MeasuringUnitType.Gkal, date, heat4hs));

            //90 Средняя температура 1 за интервал 0,01°C s16
            double temperature1 = Helper.ToInt16(data, start + 90) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура 1 за интервал"),MeasuringUnitType.C, date, temperature1));
            //92 Средняя температура 1 за интервал 0,01°C s16
            double temperature2 = Helper.ToInt16(data, start + 92) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура 2 за интервал"),MeasuringUnitType.C, date, temperature2));
            //94 Средняя температура 1 за интервал 0,01°C s16
            double temperature3 = Helper.ToInt16(data, start + 94) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура 3 за интервал"),MeasuringUnitType.C, date, temperature3));
            //96 Средняя температура 1 за интервал 0,01°C s16
            double temperature4 = Helper.ToInt16(data, start + 96) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура 4 за интервал"),MeasuringUnitType.C, date, temperature4));
            //98 Средняя температура 1 за интервал 0,01°C s16
            double temperature5 = Helper.ToInt16(data, start + 98) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура 5 за интервал"),MeasuringUnitType.C, date, temperature5));
            //100 Средняя температура хв за интервал 0,01°C s16
            double temperatureС = Helper.ToInt16(data, start + 100) / 100.0;
            Data.Add(new Data(string.Format("Средняя температура хв за интервал"),MeasuringUnitType.C, date, temperatureС));

            //102 Среднее давление 1 за интервал 0,0001 МПа u16
            double pressure1 = Helper.ToUInt16(data, start + 102) / 10.0;
            Data.Add(new Data(string.Format("Среднее давление 1 за интервал"),MeasuringUnitType.kPa, date, pressure1));
            //104 Среднее давление 2 за интервал 0,0001 МПа u16
            double pressure2 = Helper.ToUInt16(data, start + 104) / 10.0;
            Data.Add(new Data(string.Format("Среднее давление 2 за интервал"), MeasuringUnitType.kPa, date, pressure2));
            //106 Среднее давление 1 за интервал 0,0001 МПа u16
            double pressure3 = Helper.ToUInt16(data, start + 106) / 10.0;
            Data.Add(new Data(string.Format("Среднее давление 3 за интервал"), MeasuringUnitType.kPa, date, pressure3));
            //108 Среднее давление 1 за интервал 0,0001 МПа u16
            double pressure4 = Helper.ToUInt16(data, start + 108) / 10.0;
            Data.Add(new Data(string.Format("Среднее давление 4 за интервал"), MeasuringUnitType.kPa, date, pressure4));
            //110 Среднее давление хв за интервал 0,0001 МПа u16
            double pressureC = Helper.ToUInt16(data, start + 110) / 10.0;
            Data.Add(new Data(string.Format("Среднее давление хв за интервал"), MeasuringUnitType.kPa, date, pressureC));

            
            if ((index == 1) || (index == 2)) //    Суточный архив, индекс 1.             Месячный архив, индекс 2. 
            {
                //112 Время отсутствия внешнего питания за интервал мин.u16  и т.д.
                Data.Add(new Data(string.Format("Время отсутствия внешнего питания за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 112)));
                //Вместо "Время ошибок вычислений ТС" пишем в минутах  ВНР ТС
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС1 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 118)));
                Data.Add(new Data(string.Format("ВНР ТС1 за интервал"), MeasuringUnitType.min, date, 24*60 - Helper.ToUInt16(data, start + 118)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС2 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 120)));
                Data.Add(new Data(string.Format("ВНР ТС2 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 120)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС3 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 122)));
                Data.Add(new Data(string.Format("ВНР ТС3 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 122)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС4 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 124)));
                Data.Add(new Data(string.Format("ВНР ТС4 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 124)));
            }

            if (index==3) //    Часовой архив, индекс 0. 
            {
                Data.Add(new Data(string.Format("Время отсутствия внешнего питания за интервал"), MeasuringUnitType.min, date, data[112]));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС1 за интервал"), MeasuringUnitType.min, date, data[115]));
                Data.Add(new Data(string.Format("ВНР ТС1 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 115)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС2 за интервал"), MeasuringUnitType.min, date, data[116]));
                Data.Add(new Data(string.Format("ВНР ТС2 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 116)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС3 за интервал"), MeasuringUnitType.min, date, data[117]));
                Data.Add(new Data(string.Format("ВНР ТС3 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 117)));
                //Data.Add(new Data(string.Format("Время ошибок вычислений ТС4 за интервал"), MeasuringUnitType.min, date, data[118]));
                Data.Add(new Data(string.Format("ВНР ТС4 за интервал"), MeasuringUnitType.min, date, 24 * 60 - Helper.ToUInt16(data, start + 118)));
            }

            /* Сэкономим место
            Data.Add(new Data(string.Format("Время активного уровня входа 5 за интервал"), MeasuringUnitType.min, date, data[114]));
            Data.Add(new Data(string.Format("Время активного уровня входа 6 за интервал"), MeasuringUnitType.min, date, data[116]));

            Data.Add(new Data(string.Format("Время ТС1 НС1 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 126)));
            Data.Add(new Data(string.Format("Время ТС1 НС2 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 128)));
            Data.Add(new Data(string.Format("Время ТС1 НС3 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 130)));
            Data.Add(new Data(string.Format("Время ТС1 НС4 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 132)));
            Data.Add(new Data(string.Format("Время ТС2 НС1 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 134)));
            Data.Add(new Data(string.Format("Время ТС2 НС2 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 136)));
            Data.Add(new Data(string.Format("Время ТС2 НС3 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 138)));
            Data.Add(new Data(string.Format("Время ТС2 НС4 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 140)));
            Data.Add(new Data(string.Format("Время ТС3 НС1 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 142)));
            Data.Add(new Data(string.Format("Время ТС3 НС2 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 144)));
            Data.Add(new Data(string.Format("Время ТС3 НС3 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 146)));
            Data.Add(new Data(string.Format("Время ТС3 НС4 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 148)));
            Data.Add(new Data(string.Format("Время С1 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 150)));
            Data.Add(new Data(string.Format("Время С2 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 152)));
            Data.Add(new Data(string.Format("Время С3 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 154)));
            Data.Add(new Data(string.Format("Время С4 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 156)));
            Data.Add(new Data(string.Format("Время С5 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 158)));
            Data.Add(new Data(string.Format("Время С6 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 160)));
            Data.Add(new Data(string.Format("Время С7 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 162)));
            Data.Add(new Data(string.Format("Время С8 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 164)));
            Data.Add(new Data(string.Format("Время С9 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 166)));
            Data.Add(new Data(string.Format("Время С10 за интервал"), MeasuringUnitType.min, date, Helper.ToUInt16(data, start + 168)));

            //170 Состояние системы за интервал б/р u08 
            Data.Add(new Data(string.Format("Состояние системы за интервал"), MeasuringUnitType.min, date, data[170]));
            //171 Состояние измерений за интервал б / р 7 u08
            Data.Add(new Data(string.Format("Состояние измерений за интервал"), MeasuringUnitType.min, date, data[171]));
            */
        }
    }
}
