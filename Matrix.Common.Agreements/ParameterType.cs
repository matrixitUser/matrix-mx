using System;
using System.Runtime.Serialization;

namespace Matrix.Common.Agreements
{
	//    /// <summary>
	//    /// 
	//    /// </summary>
	//    [DataContract]
	//    public enum ParameterType
	//    {
	//        #region Obsolete

	//        [Obsolete]
	//        [Parameter("consumption.normal")]
	//        [EnumMember]
	//        ConsumptionNormal,

	//        [Obsolete]
	//        [EnumMember]
	//        Consumption,

	//        [Obsolete]
	//        [Parameter("consumption.work")]
	//        [EnumMember]
	//        ConsumptionWork,

	//        [Obsolete]
	//        [Parameter("consumption.mass")] //IM2300
	//        [EnumMember]
	//        ConsumptionMass,

	//        [Obsolete]
	//        [Parameter("consumption.volume")] //IM2300
	//        [EnumMember]
	//        ConsumptionVolume,

	//        [Obsolete]
	//        [Parameter("consumption.volume.normal")] //IM2300
	//        [EnumMember]
	//        ConsumptionVolumeNormal,

	//        [Obsolete]
	//        [Parameter("density")] //IM2300
	//        [EnumMember]
	//        Density,

	//        [Obsolete]
	//        [Parameter("pressure.absolute")] //IM2300
	//        [EnumMember]
	//        PressureAbsolute,

	//        [Obsolete]
	//        [Parameter("pressure.barometric")] //IM2300
	//        [EnumMember]
	//        PressureBarometric,

	//        [Obsolete]
	//        [Parameter("pressure.difference")] //IM2300
	//        [EnumMember]
	//        PressureDifference,

	//        //[Parameter("time.work")]
	//        //[EnumMember]
	//        //TimeWork,

	//        [Obsolete]
	//        [Parameter("time.turn")]
	//        [EnumMember]
	//        TimeTurn,

	//        [Obsolete]
	//        [Parameter("time.downtime")]
	//        [EnumMember]
	//        TimeDownTime,

	//        [Obsolete]
	//        [Parameter("time.emergency")]
	//        [EnumMember]
	//        TimeEmergency,

	//        [Obsolete]
	//        [Parameter("volume.checkout.time")]
	//        [EnumMember]
	//        VolumeCoT,


	//        [Parameter("Мгновенная мощность")]
	//        [EnumMember]
	//        InstantaneousPower,


	//        [Parameter("Усредненная мощность")]
	//        [EnumMember]
	//        PowerAverage,

	//        [Parameter("Энергия")]
	//        [EnumMember]
	//        Energy,

	//        [Obsolete]
	//        [Parameter("Доля Метана")]
	//        [EnumMember]
	//        Metan,
	//        [Obsolete]
	//        [Parameter("Доля Этана")]
	//        [EnumMember]
	//        Etan,
	//        [Parameter("Доля Пропана")]
	//        [EnumMember]
	//        Propan,
	//        [Parameter("Доля n-бутана")]
	//        [EnumMember]
	//        nButan,
	//        [Parameter("Доля i-бутана")]
	//        [EnumMember]
	//        iButan,
	//        [Parameter("Доля азота")]
	//        [EnumMember]
	//        azot,
	//        [Parameter("Доля углрода")]
	//        [EnumMember]
	//        uglerod,
	//        [Parameter("Доля сероводорода")]
	//        [EnumMember]
	//        serovodorod,
	//        [Parameter("Плотность газа")]
	//        [EnumMember]
	//        plotnost,
	//        [Parameter("Число Рейнольдса")]
	//        [EnumMember]
	//        reinolds,
	//        [Parameter("Плотность сухой части газа при рабочих условиях")]
	//        [EnumMember]
	//        plotnostDryWork,
	//        [Parameter("Плотность сухой части газа при стандартных условиях ")]
	//        [EnumMember]
	//        plotnostDryStd,
	//        [Parameter("Плотность влажного газа при рабочих условиях ")]
	//        [EnumMember]
	//        plotnostWetWork,
	//        [Parameter("Плотность влажного газа при стандартных условиях ")]
	//        [EnumMember]
	//        plotnostWetStd,
	//        [Parameter("Плотность влажного газа при рабочих условиях ")]
	//        [EnumMember]
	//        wetWork,
	//        [Parameter("Удельная объемная теплота сгорания ")]
	//        [EnumMember]
	//        flameTemperaure,
	//        [Parameter("Показатель адиабаты ")]
	//        [EnumMember]
	//        adiabata,
	//        [Parameter("Динамическая вязкость ")]
	//        [EnumMember]
	//        dynamicFlex,
	//        [Parameter("Коэффициент сжимаемости газа ")]
	//        [EnumMember]
	//        KoefSzh,
	//        [Parameter("Давление водяного пара на линии насыщения ")]
	//        [EnumMember]
	//        WaterP,
	//        [Parameter("Коэффициент расширения газа ")]
	//        [EnumMember]
	//        KoefRas,
	//        [Parameter("Коэффициент расхода газа ")]
	//        [EnumMember]
	//        KoefRash,
	//        [Parameter("Коэффициент шероховатости трубопровода ")]
	//        [EnumMember]
	//        KoefSheroh,
	//        [Parameter("Измеренное значение перепада давления ")]
	//        [EnumMember]
	//        dP,
	//        [Parameter("Измеренное значение давления ")]
	//        [EnumMember]
	//        PP,
	//        [Parameter("Абсолютное давление ")]
	//        [EnumMember]
	//        aP,
	//        [Parameter("Температура газа")]
	//        [EnumMember]
	//        TemperatureGaz,
	//        [Parameter("Массовый расход газа")]
	//        [EnumMember]
	//        GMass,
	//        [Parameter("Объемный расход газа при рабочих условиях")]
	//        [EnumMember]
	//        QWork,
	//        [Parameter("Объемный расход газа при стандартных условиях ")]
	//        [EnumMember]
	//        QStd,
	//        [Parameter("Масса газа нарастающим итогом")]
	//        [EnumMember]
	//        M,
	//        [Parameter("Объем газа при стандартных условиях нарастающим итогом")]
	//        [EnumMember]
	//        VStd,
	//        [Parameter("Объем газа при рабочих условиях нарастающим итогом ")]
	//        [EnumMember]
	//        VWork,
	//        [Parameter("Измеренная относительная влажность  ")]
	//        [EnumMember]
	//        Wet,
	//        [Parameter("Измеренная удельная объемная теплота сгорания")]
	//        [EnumMember]
	//        FlametmperatureIzm,
	//        [EnumMember]
	//        [Parameter("Измеренная плотность")]
	//        pIzm,
	//        [Parameter("Измеренный расход влажного газа")]
	//        [EnumMember]
	//        GWet,
	//        [Parameter("Объемный расход газа при стандартных условиях по потребителю ")]
	//        [EnumMember]
	//        Q,
	//        [Parameter("Объем газа при стандартных условиях по потребителю  ")]
	//        [EnumMember]
	//        V,
	//        [Parameter("Массовый расход газа по потребителю  ")]
	//        [EnumMember]
	//        G,
	//        [Parameter("Масса газа по потребителю")]
	//        [EnumMember]
	//        MP,
	//        [Parameter("Лимит объемного расхода газа")]
	//        [EnumMember]
	//        qLimit,
	//        [Parameter("Среднесуточная норма потребления газа")]
	//        [EnumMember]
	//        vSut,
	//        #endregion

	//        #region Общие

	//        /// <summary>
	//        /// неизвестный параметр
	//        /// </summary>
	//        [EnumMember]
	//        Unknown,

	//        /// <summary>
	//        /// Время наработки
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("ВНР")]
	//        TimeWork,

	//        /// <summary>
	//        /// Время простоя
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("ВОС")]
	//        TimeOff,

	//        /// <summary>
	//        /// Температура
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("T")]
	//        Temperature,

	//        /// <summary>
	//        /// Давление
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("P")]
	//        Pressure,

	//        /// <summary>
	//        /// перепад давления
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("ΔP")]
	//        PressureDifferential,

	//        /// <summary>
	//        /// Масса
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("M")]
	//        Mass,

	//        /// <summary>
	//        /// Объем
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("V")]
	//        Volume,

	//        /// <summary>
	//        /// Нештатная ситуация
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("НС")]
	//        Emergency,

	//        #endregion

	//        #region Obsolete

	//        /// <summary>
	//        /// тепло воды в трубе 1
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("heat.water.1")]
	//        HeatWater1,
	//        /// <summary>
	//        /// тепло воды в трубе 2
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("heat.water.2")]
	//        HeatWater2,
	//        /// <summary>
	//        /// тепло воды в трубе 3
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("heat.water.3")]
	//        HeatWater3,
	//        /// <summary>
	//        /// тепло воды в трубе 4
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("heat.water.4")]
	//        HeatWater4,

	//        /// <summary>
	//        /// тепло воды в трубе 5
	//        /// </summary>
	//        [EnumMember]
	//        HeatWater5,


	//        /// <summary>
	//        /// расход тепла
	//        /// </summary>
	//        [Parameter("heat.water.consumption")]
	//        [EnumMember]
	//        HeatWaterConsumption,

	//        /// <summary>
	//        /// температура воды в трубе 1
	//        /// </summary>
	//        [Parameter("temperature.water.1")]
	//        [EnumMember]
	//        TemperatureWater1,
	//        /// <summary>
	//        /// температура воды в трубе 2
	//        /// </summary>
	//        [Parameter("temperature.water.2")]
	//        [EnumMember]
	//        TemperatureWater2,
	//        /// <summary>
	//        /// температура воды в трубе 3
	//        /// </summary>
	//        [Parameter("temperature.water.3")]
	//        [EnumMember]
	//        TemperatureWater3,
	//        /// <summary>
	//        /// температура воды в трубе 4
	//        /// </summary>
	//        [Parameter("temperature.water.4")]
	//        [EnumMember]
	//        TemperatureWater4,
	//        /// <summary>
	//        /// температура воды в трубе 5
	//        /// </summary>
	//        [EnumMember]
	//        TemperatureWater5,

	//        /// <summary>
	//        /// температура воды холодной
	//        /// </summary>
	//        [Parameter("T ХВС")]
	//        [EnumMember]
	//        TemperatureWaterCold,

	//        /// <summary>
	//        /// разность температур (t1-t2)
	//        /// </summary>
	//        [Parameter("ΔT")]
	//        [EnumMember]
	//        TemperatureWaterConsumption,

	//        /// <summary>
	//        /// масса воды в трубе 1
	//        /// </summary>
	//        [Parameter("mass.water.1")]
	//        [EnumMember]
	//        MassWater1,
	//        /// <summary>
	//        /// масса воды в трубе 2
	//        /// </summary>
	//        [Parameter("mass.water.2")]
	//        [EnumMember]
	//        MassWater2,
	//        /// <summary>
	//        /// масса воды в трубе 3
	//        /// </summary>
	//        [Parameter("mass.water.3")]
	//        [EnumMember]
	//        MassWater3,
	//        /// <summary>
	//        /// масса воды в трубе 4
	//        /// </summary>
	//        [Parameter("mass.water.4")]
	//        [EnumMember]
	//        MassWater4,
	//        /// <summary>
	//        /// масса воды в трубе 5
	//        /// </summary>		
	//        [EnumMember]
	//        MassWater5,

	//        /// <summary>
	//        /// расход массы воды
	//        /// </summary>
	//        [Parameter("mass.water.consumption")]
	//        [EnumMember]
	//        MassWaterConsumption,

	//        /// <summary>
	//        /// давление воды в трубе 1
	//        /// </summary>
	//        [Parameter("pressure.water.1")]
	//        [EnumMember]
	//        PressureWater1,
	//        /// <summary>
	//        /// давление воды в трубе 2
	//        /// </summary>
	//        [Parameter("pressure.water.2")]
	//        [EnumMember]
	//        PressureWater2,
	//        /// <summary>
	//        /// давление воды в трубе 3
	//        /// </summary>
	//        [Parameter("pressure.water.3")]
	//        [EnumMember]
	//        PressureWater3,
	//        /// <summary>
	//        /// давление воды в трубе 4
	//        /// </summary>
	//        [Parameter("pressure.water.4")]
	//        [EnumMember]
	//        PressureWater4,
	//        /// <summary>
	//        /// давление воды в трубе 5
	//        /// </summary>		
	//        [EnumMember]
	//        PressureWater5,

	//        /// <summary>
	//        /// объем по трубе 1
	//        /// </summary>
	//        [Parameter("volume.water.1")]
	//        [EnumMember]
	//        VolumeWater1,
	//        /// <summary>
	//        /// объем по трубе 2
	//        /// </summary>
	//        [Parameter("volume.water.2")]
	//        [EnumMember]
	//        VolumeWater2,
	//        /// <summary>
	//        /// объем по трубе 3
	//        /// </summary>
	//        [Parameter("volume.water.3")]
	//        [EnumMember]
	//        VolumeWater3,
	//        /// <summary>
	//        /// объем по трубе 4
	//        /// </summary>
	//        [Parameter("volume.water.4")]
	//        [EnumMember]
	//        VolumeWater4,
	//        /// <summary>
	//        /// объем по трубе 5
	//        /// </summary>		
	//        [EnumMember]
	//        VolumeWater5,

	//        /// <summary>
	//        /// объемный расход по трубе 1
	//        /// </summary>
	//        [Parameter("volume.water.consumption.1")]
	//        [EnumMember]
	//        VolumeWaterConsumption1,
	//        /// <summary>
	//        /// объемный расход по трубе 2
	//        /// </summary>
	//        [Parameter("volume.water.consumption.2")]
	//        [EnumMember]
	//        VolumeWaterConsumption2,
	//        /// <summary>
	//        /// объемный расход по трубе 3
	//        /// </summary>
	//        [Parameter("volume.water.consumption.3")]
	//        [EnumMember]
	//        VolumeWaterConsumption3,
	//        /// <summary>
	//        /// объемный расход по трубе 4
	//        /// </summary>
	//        [Parameter("volume.water.consumption.4")]
	//        [EnumMember]
	//        VolumeWaterConsumption4,
	//        /// <summary>
	//        /// объемный расход по трубе 5
	//        /// </summary>
	//        [EnumMember]
	//        VolumeWaterConsumption5,


	//        /// <summary>
	//        /// ГВС
	//        /// </summary>
	//        [Parameter("heat.hot.water.consumption")]
	//        [EnumMember]
	//        HeatHotWaterConsumption,

	//        /// <summary>
	//        /// ХВС
	//        /// </summary>
	//        [Parameter("volume.hot.water.consumption")]
	//        [EnumMember]
	//        VolumeColdWaterConsumption,
	//        #endregion

	//        #region Тепловые счетчики

	//        /// <summary>
	//        /// тепло
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("Q")]
	//        Heat,

	//        /// <summary>
	//        /// объемный расход
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("ГВС")]
	//        VolumeConsumption,

	//        #endregion

	//        #region Газовые счетчики
	//        /// <summary>
	//        /// Объем газа при н.у.
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("Vn")]
	//        VolumeNormal,

	//        /// <summary>
	//        /// Объем газа при р.у.
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("V")]
	//        VolumeWork,

	//        /// <summary>
	//        /// Расход газа при н.у.
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("Qn")]
	//        VolumeConsumptionNormal,

	//        /// <summary>
	//        /// Расход газа при р.у.
	//        /// </summary>
	//        [EnumMember]
	//        [Parameter("Q")]
	//        VolumeConsumptionWork,

	//        #endregion
	//}
}
