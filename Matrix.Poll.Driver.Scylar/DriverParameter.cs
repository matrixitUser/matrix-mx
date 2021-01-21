using System;

namespace Matrix.Poll.Driver.Scylar
{
    enum DriverParameter
    {
        Unknown,
        EnergyWh,
        EnergyJ,
        Volume,
        Mass,
        OnTimeSeconds,
        OnTimeMinutes,
        OnTimeHours,
        OnTimeDays,
        OperatingTimeSeconds,
        OperatingTimeMinutes,
        OperatingTimeHours,
        OperatingTimeDays,
        PowerW,
        PowerJh,
        VolumeFlowm3h,
        VolumeFlowExtm3min,
        VolumeFlowExtm3s,
        MassFlow,
        FlowTemperature,
        ReturnTemperature,
        TemperatureDifferenceK,
        ExternalTemperature,
        Pressure,
        TimePoint,
        UnitHCA,
        AveragingDuration,
        ActualityDuration,
        FabricationNo,
        BusAddress,
        /// <summary>
        /// Значение энергии из расширенной таблицы (от 0.1 до 1 MWh)
        /// </summary>
        EnergyExMwh,
        /// <summary>
        /// Значение энергии из расширенной таблицы (от 0.1 до 1 GJ)
        /// </summary>
        EnergyExGJ,
        /// <summary>
        /// Значение объема из расширенной таблицы (от 100 до 1000 m3)
        /// </summary>
        VolumeExm3,
        /// <summary>
        /// Значение массы из расширенной таблицы (от 100 до 1000 t)
        /// </summary>
        MassEx,
        /// <summary>
        /// Значение объема из расширенной таблицы (0.1 feet^3)
        /// </summary>
        VolumeExFeet,
        /// <summary>
        /// Значение объема из расширенной таблицы (0.1 american galoon)
        /// </summary>
        VolumeExAgDel,
        /// <summary>
        /// Значение объема из расширенной таблицы (1 american galoon)
        /// </summary>
        VolumeExAg,
        /// <summary>
        /// Значение расхода из расширенной таблицы (0,001 american galoon/min)
        /// </summary>
        VolumeFlowExAgMinDel,
        /// <summary>
        /// Значение расхода из расширенной таблицы (american galoon/min)
        /// </summary>
        VolumeFlowExAgMin,
        /// <summary>
        /// Значение расхода из расширенной таблицы (american galoon/hour)
        /// </summary>
        VolumeFlowExAgH,
        /// <summary>
        /// Значение мощности из расширенной таблицы (от 0,1 MW до 1 MW)
        /// </summary>
        PowerExMW,
        /// <summary>
        /// Значение мощности из расширенной таблицы (от 0,1 GJ/h до 1 GJ/h)
        /// </summary>
        PowerExGJ,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        FlowTemperatureEx,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        ReturnTemperatureEx,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        TemperatureDifferenceEx,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        ExternalTemperatureEx,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        TemperatureLimitF,
        /// <summary>
        /// Значение температуры из расширенной таблицы (от 0,001 до 1 F)
        /// </summary>
        TemperatureLimitC,
        /// <summary>
        /// Счетчик максимальной мощности из расширенной таблицы (от 0,001 до 10000 W)
        /// </summary>
        CumulMaxPower,
        /// <summary>
        /// Счетчик пакетов
        /// </summary>
        AccessNumber,
        /// <summary>
        /// Как в заголовке
        /// </summary>
        Medium,
        /// <summary>
        /// Как в заголовке
        /// </summary>
        Manufacturer,   
        ParameterSetIdentification,
        Model,
        HardwareVersion,
        SoftWareVersion,
        FirmwareVersion,
        CustomerLocation,
        Customer,
        AccessCodeUser,
        AccessCodeOperator,
        AccessCodeSystemOperator,
        AccessCodeDeveloper,
        Password,
        ErrorFlags,
        ErrorMask,
        DigitalOutput,
        DigitalInput,
        Baudrate,
        ResponseDelayTime,
        Retry,
        CumulationCounter,
    }
}
