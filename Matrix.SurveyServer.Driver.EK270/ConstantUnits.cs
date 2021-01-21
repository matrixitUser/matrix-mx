using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private readonly List<MappingUnit> _constants = new List<MappingUnit>
        {
            new MappingUnit{Address="1:141.0",Description="Граница дня 1"},
            new MappingUnit{Address="1:1F0.0",Description="Длительность цикла измерения (сек)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },    //ТС220;ТС215
            new MappingUnit{Address="1:1F1.0",Description="Длительность операционного цикла (сек)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },    //ТС220;ТС215
            new MappingUnit{Address="1:207.0",Description="Режим для Входа 1",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="1:222.0",Description="Серийный номер счетчика газа",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;ТС215
            new MappingUnit{Address="1:253.0",Description="Коэффициент преобразования импульсов для входа 1 (1/м³)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },   //ТС215 ТС220            
            new MappingUnit{Address="1:407.0",Description="Режим летнее/зимнее время",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;ТС215

            new MappingUnit{Address="2:207.0",Description="Режим входа 2",Types={DevType.EK270, DevType.TC210,DevType.TC215,DevType.TC220} },    //ТС215
            new MappingUnit{Address="2:253.0",Description="Коэффициент преобразования импульсов для входа 2 (1/м³)",Types={DevType.EK270, DevType.TC210,DevType.TC215} },   //ТС215            
            new MappingUnit{Address="2:704.0",Description="Режим шины RS485",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="2:705.0",Description="Режим интерфейса",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220
            new MappingUnit{Address="2:707.0",Description="Формат данных Интерфейс 2",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="2:708.0",Description="Скорость обмена по интерфейсу",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} }, //ТС220
            new MappingUnit{Address="2:70A.0",Description="Тип интерфейса",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },    //ТС220
            new MappingUnit{Address="2:720.0",Description="Количество сигналов перед ответом",Types={DevType.EK270,DevType.EK260} },

            new MappingUnit{Address="3:424.0",Description="Температурный диапазон окружающей среды (°C)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;TC215

            new MappingUnit{Address="4:150.0",Description="Период архивации",Types={DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;ТС215
            new MappingUnit{Address="4:311.0",Description="Верхнее подстановочное значение рабочего расхода (м³/ч)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="4:315.0",Description="Нижнее подстановочное значение рабочего расхода (м³/ч)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="4:3A8.0",Description="Нижнее значение тревоги расхода (м³/ч)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="4:3A0.0",Description="Верхнее значение тревоги расхода (м³/ч)",Types={DevType.EK270,DevType.EK260} }, 

            new MappingUnit{Address="5:222.0",Description="Серийный номер датчика температуры",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },
            new MappingUnit{Address="5:223.0",Description="Тип датчика температуры",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },
            new MappingUnit{Address="5:224_1.0",Description="Нижняя граница измерения температуры (°C)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },
            new MappingUnit{Address="5:225_1.0",Description="Верхняя граница измерения температуры (°C)",Types={DevType.EK270,DevType.EK260,DevType.TC210,DevType.TC215,DevType.TC220} },            
            new MappingUnit{Address="5:311.0",Description="Подстановочное значение коэффициента коррекции, С",Types={DevType.TC210,DevType.TC215,DevType.TC220} },
            
            new MappingUnit{Address="6:212_1.0",Description="Подстановочное значение атмосферного давления (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="6:222.0",Description="Серийный номер датчика давления",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="6:223.0",Description="Тип датчика давления",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="6:224.0",Description="Нижнее значение диапазона давления (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="6:225.0",Description="Верхнее значение диапазона давления (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="6:311_1.0",Description="Подстановочное значение температуры (°C)",Types={DevType.EK270,DevType.EK260,DevType.TC210,DevType.TC215,DevType.TC220} },    //ТС220;TC215
            new MappingUnit{Address="6:312.0",Description="Стандартная температура (K)",Types={DevType.EK270,DevType.EK260,DevType.TC210,DevType.TC215,DevType.TC220} },
            new MappingUnit{Address="6:314_1.0",Description="Стандартная температура для анализа газа (°C)",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="6:317.0",Description="Режим измерения температуры",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="6:3A0_1.0",Description="Верхнее значение тревоги для температуры (°C)",Types={DevType.EK270,DevType.EK260,DevType.TC210,DevType.TC215,DevType.TC220} },  
            new MappingUnit{Address="6:3A8_1.0",Description="Нижнее значение тревоги для температуры (°C)",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },    

            new MappingUnit{Address="7:212_1.0",Description="“Коррекция 0” преобразователя перепада давления",Types={DevType.EK270} }, 
            new MappingUnit{Address="7:222.0",Description="Серийный номер преобразователя перепада",Types={DevType.EK270} }, 
            new MappingUnit{Address="7:223.0",Description="Тип преобразователя перепада давления",Types={DevType.EK270} }, 
            new MappingUnit{Address="7:224_1.0",Description="Нижнее значение диапазона давления",Types={DevType.EK270} }, 
            new MappingUnit{Address="7:225_1.0",Description="Верхнее значение диапазона давления",Types={DevType.EK270} },             
            new MappingUnit{Address="7:311.0",Description="Подстановочное значение давления",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;TC215
            new MappingUnit{Address="7:312.0",Description="Стандартное давление",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },  //ТС220;TC215
            new MappingUnit{Address="7:314_1.0",Description="Стандартное давление для анализа газа (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="7:317.0",Description="Режим измерения давления",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="7:3A0.0",Description="Верхнее значение тревоги (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="7:3A8.0",Description="Нижнее значение тревоги (бар)",Types={DevType.EK270,DevType.EK260} },             

            new MappingUnit{Address="8:150.0",Description="Нижнее значение предупреждения Qр (м³/ч)",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="8:154.0",Description="Наблюдение Qр",Types={DevType.EK270,DevType.EK260} },
            new MappingUnit{Address="8:158.0",Description="Верхнее значение предупреждения Qр (м³/ч)",Types={DevType.EK270,DevType.EK260} },            
            new MappingUnit{Address="8:311.0",Description="Подстановочное значение К",Types={DevType.EK270,DevType.EK260, DevType.TC210,DevType.TC215,DevType.TC220} },
            new MappingUnit{Address="8:317.0",Description="Режим вычисления К",Types={DevType.EK270,DevType.EK260} },
                
            new MappingUnit{Address="9:150.0",Description="Нижнее значение предупреждения (°C)" ,Types={DevType.EK270,DevType.EK260}},
            new MappingUnit{Address="9:158.0",Description="Верхнее значение предупреждения (°C)" ,Types={DevType.EK270,DevType.EK260}},
            new MappingUnit{Address="9:311.0",Description="Коэф. реального газа z"},
            
            new MappingUnit{Address="10:150.0",Description="Нижнее значение предупреждения (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="10:158.0",Description="Верхнее значение предупреждения (бар)",Types={DevType.EK270,DevType.EK260} }, 
            new MappingUnit{Address="10:311.0",Description="Теплота сгорания Ho,с"},
            new MappingUnit{Address="10:312.0",Description="Теплота сгорания (кВт*ч/м³)",Types={DevType.EK270,DevType.EK260}},            

            new MappingUnit{Address="11:150.0",Description="Предел 1 для наблюдения Входа 2",Types={DevType.EK270}},
            new MappingUnit{Address="11:154.0",Description="Источник для наблюдения Входа 2",Types={DevType.EK270}},
            new MappingUnit{Address="11:157.0",Description="Режим для наблюдения на Входе 2",Types={DevType.EK270}},
            new MappingUnit{Address="11:311.0",Description="Содержание двуокиси углерода CO₂",IsComposition=true},
            new MappingUnit{Address="11:314.0",Description="Содержание двуокиси углерода CO₂ (%)",IsComposition=true,Types={DevType.EK270,DevType.EK260}},
            
            new MappingUnit{Address="12:150.0",Description="Предел 1 для наблюдения Входа 3",Types={DevType.EK270}},
            new MappingUnit{Address="12:154.0",Description="Источник для наблюдения Входа 3",Types={DevType.EK270}},
            new MappingUnit{Address="12:157.0",Description="Режим для наблюдения на Входе 3",Types={DevType.EK270}},
            new MappingUnit{Address="12:311.0",Description="Содержание водорода H₂",IsComposition=true},
            new MappingUnit{Address="12:314.0",Description="Содержание водорода H₂ (%)",IsComposition=true,Types={DevType.EK270,DevType.EK260}},
            
            new MappingUnit{Address="13:311.0",Description="Стандартная плотность Rhoс"},                
            new MappingUnit{Address="13:314_1.0",Description="Стандартная плотность газа (кг/м³)",Types={DevType.EK270,DevType.EK260}},

            new MappingUnit{Address="14:314.0",Description="Содержание азота N₂ (%)",IsComposition=true,Types={DevType.EK270,DevType.EK260}},

            new MappingUnit{Address="15:314.0",Description="Относительная плотность газа",Types={DevType.EK270,DevType.EK260}},

            new MappingUnit{Address="20:150.0",Description="Нижнее значение предупреждения (кПа)",Types={DevType.EK270}},
            new MappingUnit{Address="20:158.0",Description="Верхнее значение предупреждения (кПа)",Types={DevType.EK270}},
        };
    }
}
