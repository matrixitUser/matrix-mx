using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver; //*


namespace Matrix.SurveyServer.Driver.TSRV023
{
    /// <summary>
    /// 
    /// </summary>
    class Response65 : Response
    {
        public List<Data> Data { get; private set; }

        public int Channel { get; private set; }

        public Response65(byte[] data, BaseDriver myDriver)
            : base(data)
        {
            Data = new List<Common.Data>();

            var length = data[2];
            int start = 3; //0,1,2

            var seconds = Helper.ToUInt32(data, start + 0);  //3,4,5,6

            //если данные нулевые, игнорим их
            if (seconds == 0) return;

            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);
            Driver D = new Driver();

            byte[] bytes = null;

            //По теплосистемам  Документ: sa_tsrv_023
            for (int nHeatSystem = 1; nHeatSystem <= 3; nHeatSystem++)
            {
                bytes = data.Skip(3 + 4 + (nHeatSystem - 1) * 44).Take(44).ToArray();  //7-2=5, 51-2 =49,  и далее
                heatSystem(date, bytes, nHeatSystem, myDriver);
            }

            //По каналам  Документ: sa_tsrv_023
            for (int nChannel = 0; nChannel <= 6; nChannel++)
            {
                bytes = data.Skip(3 + 136 + (nChannel - 1) * 14).Take(14).ToArray();   //139, 153 и далее
                byChannel(date, bytes, nChannel, myDriver);
            }

            //Суммарное тепло
            var heatSumma = Helper.ToSingle(data, 3 + 234);
            Data.Add(new Data(string.Format("Суммарное тепло"), MeasuringUnitType.Gkal, date, heatSumma));


            for (int nElement = 0; nElement <= (Data.Count - 1); nElement++)
            {
                myDriver.OnSendMessage(string.Format(" {0} Value :{1} {2}", Data[nElement].ParameterName, Data[nElement].Value, Data[nElement].MeasuringUnit));
            }

        }

        private void heatSystem(DateTime date, byte[] bytes, int nHeatSystem, BaseDriver myDriver)
        {
            //Структура архивной записи теплосистемы sa_tsrv_023.pdf
            try
            {
                var heat1 = Helper.ToSingle(bytes, 0) / 4.1868;
                Data.Add(new Data(string.Format("Теплосистема {0} Теплота 1", nHeatSystem), MeasuringUnitType.Gkal, date, heat1));

                var heat2 = Helper.ToSingle(bytes, 4) / 4.1868;
                Data.Add(new Data(string.Format("Теплосистема {0} Теплота 2", nHeatSystem), MeasuringUnitType.Gkal, date, heat2));

                var heat3 = Helper.ToSingle(bytes, 8) / 4.1868;
                Data.Add(new Data(string.Format("Теплосистема {0} Теплота 3", nHeatSystem), MeasuringUnitType.Gkal, date, heat3));

                var timeWork = Helper.ToUInt32(bytes, 12);
                Data.Add(new Data(string.Format("Теплосистема {0} Время работы", nHeatSystem), MeasuringUnitType.min, date, timeWork));

                var timeIdle = Helper.ToUInt32(bytes, 16);
                Data.Add(new Data(string.Format("Теплосистема {0} Время простоя", nHeatSystem), MeasuringUnitType.min, date, timeIdle));

                var timeEmergency1 = Helper.ToUInt32(bytes, 20);
                Data.Add(new Data(string.Format("Теплосистема {0} Время нештатной ситуации 1", nHeatSystem), MeasuringUnitType.min, date, timeEmergency1));

                var timeEmergency2 = Helper.ToUInt32(bytes, 24);
                Data.Add(new Data(string.Format("Теплосистема {0} Время нештатной ситуации 2", nHeatSystem), MeasuringUnitType.min, date, timeEmergency2));

                var timeEmergency3 = Helper.ToUInt32(bytes, 28);
                Data.Add(new Data(string.Format("Теплосистема {0} Время нештатной ситуации 3", nHeatSystem), MeasuringUnitType.min, date, timeEmergency3));

                var timeEmergencyHC0 = Helper.ToUInt32(bytes, 32);
                Data.Add(new Data(string.Format("Теплосистема {0} Время нештатной ситуации HC0", nHeatSystem), MeasuringUnitType.min, date, timeEmergencyHC0));

                var statusWord = Helper.ToUInt32(bytes, 40);
                Data.Add(new Data(string.Format("Теплосистема {0} Слово состояние", nHeatSystem), MeasuringUnitType.min, date, statusWord));

            }
            catch (Exception ex2)
            {
                myDriver.OnSendMessage(string.Format(string.Format("ошибка в heatSystem: {0}", ex2.Message)));
            }


        }

        private void byChannel(DateTime date, byte[] bytes, int nChannel, BaseDriver myDriver)
        {
            //Структура архивной записи канала sa_tsrv_023.pdf
            try
            {
                var mass = Helper.ToSingle(bytes, 0) / 1000f;
                Data.Add(new Data(string.Format("Масса по каналу {0}", nChannel), MeasuringUnitType.tonn, date, mass));

                var temperature = (double)Helper.ToInt16(bytes, 4) / 100.0;
                Data.Add(new Data(string.Format("Температура по каналу {0}", nChannel), MeasuringUnitType.C, date, temperature));

                var pressure = (double)Helper.ToUInt16(bytes, 6) / 100.0;
                Data.Add(new Data(string.Format("Давление по каналу {0}", nChannel), MeasuringUnitType.MPa, date, pressure));

                var volume = Helper.ToUInt32(bytes, 8);
                Data.Add(new Data(string.Format("Суммарный объем по каналу {0}", nChannel), MeasuringUnitType.m3, date, volume));

                var statusWord = Helper.ToUInt16(bytes, 12);
                Data.Add(new Data(string.Format("Слово состояние по каналу {0}", nChannel), MeasuringUnitType.min, date, statusWord));

            }
            catch (Exception ex2)
            {
                myDriver.OnSendMessage(string.Format(string.Format("ошибка в byChannel: {0}", ex2.Message)));
            }
        }

    }
}
