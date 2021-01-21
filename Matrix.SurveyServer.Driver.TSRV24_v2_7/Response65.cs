﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TSRV24
{
	/// <summary>
	/// 
	/// </summary>
	class Response65 : Response
	{
		public List<Data> Data { get; private set; }

		public int Channel { get; private set; }

        public string Text { get; private set; }

		public Response65(byte[] data, int channel)
			: base(data)
		{
            Text = "";

			Channel = channel;

			Data = new List<Common.Data>();

			var length = data[2];
			int start = 3;

			var seconds = Helper.ToUInt32(data, start + 0);

			//если данные нулевые, игнорим их
			if (seconds == 0) return;

            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);

            Text += string.Format("{0:dd.MM.yy HH:mm} ", date);

			var timeWork = Helper.ToUInt16(data, start + 4);
			Data.Add(new Data(string.Format("Время работы в штатном режиме (ТС {0})", Channel), MeasuringUnitType.min, date, timeWork));

			var timeOff = Helper.ToUInt16(data, start + 10);
			Data.Add(new Data(string.Format("Общее время простоя из-за НС (ТС {0})", Channel), MeasuringUnitType.min, date, timeOff));

			var heat1 = Helper.ToSingle(data, start + 96);
            if(!float.IsNaN(heat1))
            {
                Data.Add(new Data(string.Format("Тепло по трубе 1 (ТС {0})", Channel), MeasuringUnitType.Gkal, date, heat1));
                Text += string.Format("{0}={1}; ", Data.Last().ParameterName, Data.Last().Value);
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Тепло по трубе 1 (ТС {0})", Channel));
            }

            var heat2 = Helper.ToSingle(data, start + 100);
            if (!float.IsNaN(heat2))
            {
                Data.Add(new Data(string.Format("Тепло по трубе 2 (ТС {0})", Channel), MeasuringUnitType.Gkal, date, heat2));
                Text += string.Format("{0}={1}; ", Data.Last().ParameterName, Data.Last().Value);
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Тепло по трубе 2 (ТС {0})", Channel));
            }

            var heat3 = Helper.ToSingle(data, start + 104);
            if (!float.IsNaN(heat3))
            {
                Data.Add(new Data(string.Format("Тепло по трубе 3 (ТС {0})", Channel), MeasuringUnitType.Gkal, date, heat3));
                Text += string.Format("{0}={1}; ", Data.Last().ParameterName, Data.Last().Value);
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Тепло по трубе 3 (ТС {0})", Channel));
            }


			var heat4 = Helper.ToSingle(data, start + 108);
            if (!float.IsNaN(heat4))
            {
                Data.Add(new Data(string.Format("Тепло по трубе 4 (ТС {0})", Channel), MeasuringUnitType.Gkal, date, heat4));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Тепло по трубе 4 (ТС {0})", Channel));
            }

			var mass1 = Helper.ToSingle(data, start + 112);
            if (!float.IsNaN(mass1))
            {
                Data.Add(new Data(string.Format("Масса по трубе 1 (ТС {0})", Channel), MeasuringUnitType.tonn, date, mass1));
                Text += string.Format("{0}={1}; ", Data.Last().ParameterName, Data.Last().Value);
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Масса по трубе 1 (ТС {0})", Channel));
            }

			var mass2 = Helper.ToSingle(data, start + 116);
            if (!float.IsNaN(mass2))
            {
                Data.Add(new Data(string.Format("Масса по трубе 2 (ТС {0})", Channel), MeasuringUnitType.tonn, date, mass2));
                Text += string.Format("{0}={1}; ", Data.Last().ParameterName, Data.Last().Value);
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Масса по трубе 2 (ТС {0})", Channel));
            }

			var mass3 = Helper.ToSingle(data, start + 120);
            if (!float.IsNaN(mass3))
            {
			    Data.Add(new Data(string.Format("Масса по трубе 3 (ТС {0})", Channel), MeasuringUnitType.tonn, date, mass3));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Масса по трубе 3 (ТС {0})", Channel));
            }

			var mass4 = Helper.ToSingle(data, start + 124);
            if (!float.IsNaN(mass4))
            {
                Data.Add(new Data(string.Format("Масса по трубе 4 (ТС {0})", Channel), MeasuringUnitType.tonn, date, mass4));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Масса по трубе 4 (ТС {0})", Channel));
            }

			var vol1 = Helper.ToSingle(data, start + 128);
            if (!float.IsNaN(vol1))
            {
                Data.Add(new Data(string.Format("Объем по трубе 1 (ТС {0})", Channel), MeasuringUnitType.m3, date, vol1));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Объем по трубе 1 (ТС {0})", Channel));
            }

			var vol2 = Helper.ToSingle(data, start + 132);
            if (!float.IsNaN(vol2))
            {
                Data.Add(new Data(string.Format("Объем по трубе 2 (ТС {0})", Channel), MeasuringUnitType.m3, date, vol2));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Объем по трубе 2 (ТС {0})", Channel));
            }

			var vol3 = Helper.ToSingle(data, start + 136);
            if (!float.IsNaN(vol3))
            {
                Data.Add(new Data(string.Format("Объем по трубе 3 (ТС {0})", Channel), MeasuringUnitType.m3, date, vol3));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Объем по трубе 3 (ТС {0})", Channel));
            }

			var vol4 = Helper.ToSingle(data, start + 140);
            if (!float.IsNaN(vol4))
            {
                Data.Add(new Data(string.Format("Объем по трубе 4 (ТС {0})", Channel), MeasuringUnitType.m3, date, vol4));
            }
            else
            {
                Text += string.Format("{0} не является числом; ", string.Format("Объем по трубе 4 (ТС {0})", Channel));
            }


			var temperature1 = Helper.ToInt16(data, start + 152) / 100;
			Data.Add(new Data(string.Format("Температура по трубе 1 (ТС {0})", Channel), MeasuringUnitType.C, date, temperature1));

			var temperature2 = Helper.ToInt16(data, start + 154) / 100;
			Data.Add(new Data(string.Format("Температура по трубе 2 (ТС {0})", Channel), MeasuringUnitType.C, date, temperature2));

			var temperature3 = Helper.ToInt16(data, start + 156) / 100;
			Data.Add(new Data(string.Format("Температура по трубе 3 (ТС {0})", Channel), MeasuringUnitType.C, date, temperature3));

			var temperature4 = Helper.ToInt16(data, start + 158) / 100;
			Data.Add(new Data(string.Format("Температура по трубе 4 (ТС {0})", Channel), MeasuringUnitType.C, date, temperature4));

			var pressure1 = Helper.ToUInt16(data, start + 160) / 1000;
			Data.Add(new Data(string.Format("Давление по трубе 1 (ТС {0})", Channel), MeasuringUnitType.MPa, date, pressure1));

			var pressure2 = Helper.ToUInt16(data, start + 162) / 1000;
			Data.Add(new Data(string.Format("Давление по трубе 2 (ТС {0})", Channel), MeasuringUnitType.MPa, date, pressure2));

			var pressure3 = Helper.ToUInt16(data, start + 164) / 1000;
			Data.Add(new Data(string.Format("Давление по трубе 3 (ТС {0})", Channel), MeasuringUnitType.MPa, date, pressure3));

			var pressure4 = Helper.ToUInt16(data, start + 166) / 1000;
            Data.Add(new Data(string.Format("Давление по трубе 4 (ТС {0})", Channel), MeasuringUnitType.MPa, date, pressure3));

			Data.Add(new Data(string.Format("dV (ТС {0})", Channel), MeasuringUnitType.m3, date, vol1 - vol2));
		}
	}
}
