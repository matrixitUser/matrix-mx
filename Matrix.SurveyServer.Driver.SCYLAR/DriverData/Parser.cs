using System;
using System.Collections.Generic;
using System.Linq;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
	public static class Parser
	{
		public static Tuple<DateTime, IEnumerable<Data>> Parse(byte[] data)
		{
			if (data.Length < 15) return null;
			var cField = data[0];
			if (cField != 0x08 && cField != 0x18 && cField != 0x28 && cField != 0x38)
				return null;

			var ciField = data[2];
			if (ciField != 0x72 && ciField != 0x76)
				return null;
			//первые 12 байт - заголовок. Пропускаем его, там нет ничего интересного

			int currentIndex = 15;
			var records = new List<VariableDataBlock>();

			while (true)
			{
				var vdb = VariableDataBlock.Parse(data, currentIndex);
				if (vdb == null) break;
				records.Add(vdb);

				currentIndex += vdb.Length;
				if (currentIndex >= data.Length) break;
			}


			return ConvertToData(records);
		}

		private static readonly MapCollection mapper = new MapCollection
        {
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.EnergyExGJ,
					DriverParameter.EnergyExMwh,
					DriverParameter.EnergyWh,
					DriverParameter.EnergyJ
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Heat",
					Channel=1,
					CalculationType=CalculationType.Total
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.EnergyExGJ,
					DriverParameter.EnergyExMwh,
					DriverParameter.EnergyWh,
					DriverParameter.EnergyJ,
				}, 1), 
				new UglyParameter
				{
					ParameterType = "Heat",
					Channel=3,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.TemperatureDifferenceK,
					DriverParameter.TemperatureDifferenceEx
				}, 0), 
				new UglyParameter
				{
					ParameterType = "TemperatureWaterConsumption",
                    Channel = 0,
                    CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.FlowTemperature,
					DriverParameter.FlowTemperatureEx
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Temperature",
					Channel=1,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.FlowTemperature,
					DriverParameter.FlowTemperatureEx
				}, 1),
				new UglyParameter
				{
					ParameterType = "Temperature",
					Channel=3,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.ReturnTemperature,
					DriverParameter.ReturnTemperatureEx
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Temperature",
					Channel=2,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.ReturnTemperature,
					DriverParameter.ReturnTemperatureEx
				}, 1), 
				new UglyParameter
				{
					ParameterType = "Temperature",
					Channel=4,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.Mass
					,DriverParameter.MassEx
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Mass",
					Channel=1,
					CalculationType=CalculationType.Total
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.Mass
					,DriverParameter.MassEx
				}, 1), 
				new UglyParameter
				{
					ParameterType = "Mass",
					Channel=3,
					CalculationType=CalculationType.Total
				}
			},
            
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.Volume,
					DriverParameter.VolumeExAg,
					DriverParameter.VolumeExFeet,
					DriverParameter.VolumeExm3
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Volume",
					Channel=1,
					CalculationType=CalculationType.Total
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.Volume,
					DriverParameter.VolumeExAg,
					DriverParameter.VolumeExFeet,
					DriverParameter.VolumeExm3
				}, 1), 
				new UglyParameter
				{
					ParameterType = "Volume",
					Channel=3,
					CalculationType=CalculationType.Total
				}
			},
			//{
			//    new CompositeKey(new List<DriverParameter>
			//    {
			//        DriverParameter.Mass
			//        ,DriverParameter.MassEx
			//    }, 0 , 1), 
			//    new UglyParameter
			//    {
			//        ParameterType = ParameterType.Mass,
			//        Channel=1,
			//        CalculationType=CalculationType.NotCalculated
			//    }
			//},
			//{
			//    new CompositeKey(new List<DriverParameter>
			//    {
			//        DriverParameter.Mass
			//        ,DriverParameter.MassEx
			//    }, 1,1), 
			//    new UglyParameter
			//    {
			//        ParameterType = ParameterType.Mass,
			//        Channel=3,
			//        CalculationType=CalculationType.NotCalculated
			//    }
			//},
			//{
			//    new CompositeKey(new List<DriverParameter>
			//    {
			//        DriverParameter.Volume,
			//        DriverParameter.VolumeExAg,
			//        DriverParameter.VolumeExFeet,
			//        DriverParameter.VolumeExm3
			//    }, 0,1), 
			//    new UglyParameter
			//    {
			//        ParameterType = ParameterType.Volume,
			//        Channel=1,
			//        CalculationType=CalculationType.NotCalculated
			//    }
			//},
			//{
			//    new CompositeKey(new List<DriverParameter>
			//    {
			//        DriverParameter.Volume,
			//        DriverParameter.VolumeExAg,
			//        DriverParameter.VolumeExFeet,
			//        DriverParameter.VolumeExm3
			//    }, 1,1), 
			//    new UglyParameter
			//    {
			//        ParameterType = ParameterType.Volume,
			//        Channel=3,
			//        CalculationType=CalculationType.NotCalculated
			//    }
			//},
			//{new CompositeKey(new List<DriverParameter>{DriverParameter.MassFlow}, 1),new UglyParameter{ParameterType = ParameterType.Volume,CalculationType=CalculationType.NotCalculated,Channel=1}},
			//{new CompositeKey(new List<DriverParameter>{DriverParameter.MassFlow}, 2),new UglyParameter{ParameterType = ParameterType.Volume,CalculationType=CalculationType.NotCalculated,Channel=2}},
			//{new CompositeKey(new List<DriverParameter>{DriverParameter.MassFlow}, 3),new UglyParameter{ParameterType = ParameterType.Volume,CalculationType=CalculationType.NotCalculated,Channel=3}},
			//{new CompositeKey(new List<DriverParameter>{DriverParameter.MassFlow}, 4),new UglyParameter{ParameterType = ParameterType.Volume,CalculationType=CalculationType.NotCalculated,Channel=4}},
			
			//{
			//    new CompositeKey(new List<DriverParameter>
			//    {
			//        DriverParameter.VolumeFlowExAgH
			//        ,DriverParameter.VolumeFlowExAgMin
			//        ,DriverParameter.VolumeFlowExAgMinDel
			//        ,DriverParameter.VolumeFlowExtm3min
			//        ,DriverParameter.VolumeFlowExtm3s
			//        ,DriverParameter.VolumeFlowm3h
			//    }, 0), 
			//    new UglyParameter
			//    {
			//        ParameterType=ParameterType.Volume,
			//        CalculationType = CalculationType.NotCalculated,
			//        Channel=1
			//    }
			//},
			//{new CompositeKey(new List<DriverParameter>
			//{
			//    DriverParameter.VolumeFlowExAgH
			//    ,DriverParameter.VolumeFlowExAgMin
			//    ,DriverParameter.VolumeFlowExAgMinDel
			//    ,DriverParameter.VolumeFlowExtm3min
			//    ,DriverParameter.VolumeFlowExtm3s
			//    ,DriverParameter.VolumeFlowm3h
			//}, 1), ParameterType.VolumeWaterConsumption2},
			//{new CompositeKey(new List<DriverParameter>
			//{
			//    DriverParameter.VolumeFlowExAgH
			//    ,DriverParameter.VolumeFlowExAgMin
			//    ,DriverParameter.VolumeFlowExAgMinDel
			//    ,DriverParameter.VolumeFlowExtm3min
			//    ,DriverParameter.VolumeFlowExtm3s
			//    ,DriverParameter.VolumeFlowm3h
			//}, 2), ParameterType.VolumeWaterConsumption3},
			//{new CompositeKey(new List<DriverParameter>
			//{
			//    DriverParameter.VolumeFlowExAgH
			//    ,DriverParameter.VolumeFlowExAgMin
			//    ,DriverParameter.VolumeFlowExAgMinDel
			//    ,DriverParameter.VolumeFlowExtm3min
			//    ,DriverParameter.VolumeFlowExtm3s
			//    ,DriverParameter.VolumeFlowm3h
			//}, 3), ParameterType.VolumeWaterConsumption4},
			//{new CompositeKey(new List<DriverParameter>
			//{
			//    DriverParameter.PowerExGJ
			//    ,DriverParameter.PowerExMW
			//    ,DriverParameter.PowerW
			//    ,DriverParameter.PowerJh
			//}, 0), ParameterType.Power},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.OnTimeDays,
					DriverParameter.OnTimeHours,
					DriverParameter.OnTimeMinutes,
					DriverParameter.OnTimeSeconds
				}, 0), 
				new UglyParameter
				{
					ParameterType = "TimeWork",
					Channel=0,
					CalculationType=CalculationType.Total
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.OperatingTimeDays,
					DriverParameter.OperatingTimeHours,
					DriverParameter.OperatingTimeMinutes,
					DriverParameter.OperatingTimeSeconds
				}, 0), 
				new UglyParameter
				{
					ParameterType = "TimeTurn",
					Channel=0,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.Pressure
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Pressure",
					Channel=0,
					CalculationType=CalculationType.NotCalculated
				}
			},
            {
				new CompositeKey(new List<DriverParameter>
				{
					DriverParameter.ErrorFlags
				}, 0), 
				new UglyParameter
				{
					ParameterType = "Emergency",
					Channel=0,
					CalculationType=CalculationType.NotCalculated
				}
			},
        };

		private static readonly Dictionary<DriverParameter, MeasuringUnitType> dimensionMapper = new Dictionary
			<DriverParameter, MeasuringUnitType>
                    {
                        {DriverParameter.EnergyExGJ,MeasuringUnitType.GDj},
                        {DriverParameter.EnergyExMwh, MeasuringUnitType.MWtH},
                        {DriverParameter.EnergyJ, MeasuringUnitType.Dj},
                        {DriverParameter.EnergyWh, MeasuringUnitType.WtH},
                        {DriverParameter.TemperatureDifferenceK, MeasuringUnitType.C},
                        {DriverParameter.FlowTemperature, MeasuringUnitType.C},
                        {DriverParameter.FlowTemperatureEx, MeasuringUnitType.C},
                        {DriverParameter.ReturnTemperature, MeasuringUnitType.C},
                        {DriverParameter.ReturnTemperatureEx, MeasuringUnitType.C},
                        {DriverParameter.MassEx, MeasuringUnitType.tonn },
                        {DriverParameter.Mass, MeasuringUnitType.kg},
                        {DriverParameter.Volume, MeasuringUnitType.kubM},
                        {DriverParameter.VolumeExm3, MeasuringUnitType.kubM},
                        {DriverParameter.VolumeFlowm3h, MeasuringUnitType.kubM_h},
                        {DriverParameter.PowerW, MeasuringUnitType.Wt},
                        {DriverParameter.PowerExGJ, MeasuringUnitType.GDj},
                        {DriverParameter.PowerExMW, MeasuringUnitType.MWt},
                        {DriverParameter.OnTimeDays, MeasuringUnitType.day},
                        {DriverParameter.OnTimeHours, MeasuringUnitType.h},
                        {DriverParameter.OnTimeMinutes, MeasuringUnitType.min},
                        {DriverParameter.OnTimeSeconds, MeasuringUnitType.sec},
                        {DriverParameter.OperatingTimeDays, MeasuringUnitType.day},
                        {DriverParameter.OperatingTimeHours, MeasuringUnitType.h},
                        {DriverParameter.OperatingTimeMinutes, MeasuringUnitType.min},
                        {DriverParameter.OperatingTimeSeconds, MeasuringUnitType.sec},
                        {DriverParameter.Pressure, MeasuringUnitType.Bar},
                    };

		private static Tuple<DateTime, IEnumerable<Data>> ConvertToData(IEnumerable<VariableDataBlock> collection)
		{
			if (collection == null) return null;
			var colList = collection.ToList();
			var list = new List<VariableDataBlock>();

			//будем использовать только те элементы, значения unit'а которых будет наибольшим
			foreach (var variableDataBlock in colList)
			{
				//if (colList.Any(c => c.Value is double
				//    && (double)c.Value > 0
				//    && c.DriverParameter == variableDataBlock.DriverParameter
				//    && c.Drh.Dib.Tarrif == variableDataBlock.Drh.Dib.Tarrif
				//    && c.Drh.Dib.StorageNumber == variableDataBlock.Drh.Dib.StorageNumber
				//    && c.Drh.Dib.Unit > variableDataBlock.Drh.Dib.Unit))
				//    continue;

				list.Add(variableDataBlock);
			}

			var dateTimeRecord = list.FirstOrDefault(vdb => vdb.DriverParameter == DriverParameter.TimePoint);
			if (dateTimeRecord == null || !(dateTimeRecord.Value is DateTime))
				return null;

			var recordDateTime = (DateTime)dateTimeRecord.Value;
			var recordList = new List<Data>();

			foreach (var dataBlock in list)
			{
				var uglyParameter = mapper[dataBlock.DriverParameter, dataBlock.Drh.Dib.Tarrif, dataBlock.Drh.Dib.Unit];
				//ParameterType parameterType = //mapper[dataBlock.DriverParameter, dataBlock.Drh.Dib.Tarrif];
				if (uglyParameter.Equals(UglyParameter.Unknown)) continue;
				MeasuringUnitType measuringUnitType = MeasuringUnitType.Unknown;
				if (dimensionMapper.ContainsKey(dataBlock.DriverParameter))
					measuringUnitType = dimensionMapper[dataBlock.DriverParameter];
				var value = (double)dataBlock.Value;
				recordList.Add(new Data(string.Format("{0}{1}{2}", uglyParameter.ParameterType, uglyParameter.Channel, uglyParameter.CalculationType), measuringUnitType, recordDateTime, value));
			}

			return new Tuple<DateTime, IEnumerable<Data>>(recordDateTime, recordList);
		}

		public static int FromBcdToInt(byte bcd)
		{
			return 0;
		}
		public static int FromBcdToInt(byte[] buff, int startIndex, int length)
		{
			int result = 0;

			if (buff == null || buff.Length < startIndex + length)
				return result;

			for (int i = startIndex + length - 1; i >= startIndex; i--)
			{
				result *= 100;
				result += 10 * ((buff[i] & 0xF0) >> 4) + (buff[i] & 0x0F);
			}
			return result;
		}
	}
}
