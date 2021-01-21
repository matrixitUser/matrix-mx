using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        string[] CurrentList = new string[]
        {
            "t1 Тв1",
            "t2 Тв1",
            "t3 Тв1",
            "P1 Тв1",
            "P2 Тв1",
            "dt Тв1",
            "tх",
            "ta",
            "G1 Тв1",
            "G2 Тв1",
            "G3 Тв1",
            "t1 Тв2",
            "t2 Тв2",
            "t3 Тв2",
            "P1 Тв2",
            "P2 Тв2",
            "dt Тв2",
            "G1 Тв2",
            "G2 Тв2",
            "G3 Тв2",
            "Наличие нештатной ситуации по ТВ1",
            "Наличие нештатной ситуации по ТВ2",
            "DI",
            "P3"
        };

        string[] TotalList = new string[] 
        {
            "V1 Тв1",
            "V2 Тв1",
            "V3 Тв1",
            "M1 Тв1",
            "M2 Тв1",
            "M3 Тв1",
            "Mг Тв1",
            "Qо Тв1",
            "Qг Тв1",
            "BНP Тв1",
            "BOC Тв1",
            "V1 Тв2",
            "V2 Тв2",
            "V3 Тв2",
            "M1 Тв2",
            "M2 Тв2",
            "M3 Тв2",
            "Mг Тв2",
            "Qо Тв2",
            "Qг Тв2",
            "BНP Тв2",
            "BOC Тв2",
            "DI"
        };

        string[] ArchiveList = new string[]
        {
            "t1 Тв1",
            "t2 Тв1",
            "t3 Тв1",
            "V1 Тв1",
            "V2 Тв1",
            "V3 Тв1",
            "M1 Тв1",
            "M2 Тв1",
            "M3 Тв1",
            "P1 Тв1",
            "P2 Тв1",
            "Mг Тв1",
            "Qо Тв1",
            "Qг Тв1",
            "dt Тв1",
            "tх",
            "ta",
            "BНP Тв1",
            "BOC Тв1",
            "t1 Тв2",
            "t2 Тв2",
            "t3 Тв2",
            "V1 Тв2",
            "V2 Тв2",
            "V3 Тв2",
            "M1 Тв2",
            "M2 Тв2",
            "M3 Тв2",
            "P1 Тв2",
            "P2 Тв2",
            "Mг Тв2",
            "Qо Тв2",
            "Qг Тв2",
            "dt Тв2",
            "BНP Тв2",
            "BOC Тв2",
            "Наличие нештатной ситуации по ТВ1",
            "Наличие нештатной ситуации по ТВ2",
            "Длительность НС по параметрам Тв1",
            "Длительность НС по параметрам Тв2",
            "DI",
            "P3"
        };

        //FILTER elements
        //A+0 t1_1Type t1 Тв1 параметр
        //A+1 t2_1Type t2 Тв1 параметр
        //A+2 t3_1Type t3 Тв1 параметр
        //AT3 V1_1Type V1 Тв1 параметр
        //AT4 V2_1Type V2 Тв1 параметр
        //AT5 V3_1Type V3 Тв1 параметр
        //AT6 M1_1Type M1 Тв1 параметр
        //AT7 M2_1Type M2 Тв1 параметр
        //AT8 M3_1Type M3 Тв1 параметр
        //A+9 P1_1Type P1 Тв1 параметр
        //A+10 P2_1Type P2 Тв1 параметр
        //AT11 Mg_1TypeP Mг Тв1 параметр
        //AT12 Qo_1TypeP Qо Тв1 параметр
        //AT13 Qg_1TypeP Qг Тв1 параметр
        //A+14 dt_1TypeP dt Тв1 параметр
        //A+15 tswTypeP tх параметр
        //A+16 taTypeP ta параметр
        //AT17 QntType_1HIP BНP Тв1 параметр
        //AT18 QntType_1P BOC Тв1 параметр
        //+19 G1Type G1 Тв1 параметр
        //+20 G2Type G2 Тв1 параметр
        //+21 G3Type G3 Тв1 параметр
        //A+22 t1_2Type t1 Тв2 параметр
        //A+23 t2_2Type t2 Тв2 параметр
        //A+24 t3_2Type t3 Тв2 параметр
        //AT25 V1_2Type V1 Тв2 параметр
        //AT26 V2_2Type V2 Тв2 параметр
        //AT27 V3_2Type V3 Тв2 параметр
        //AT28 M1_2Type M1 Тв2 параметр
        //AT29 M2_2Type M2 Тв2 параметр
        //AT30 M3_2Type M3 Тв2 параметр
        //A+31 P1_2Type P1 Тв2 параметр
        //A+32 P2_2Type P2 Тв2 параметр
        //AT33 Mg_2TypeP Mг Тв2 параметр
        //AT34 Qo_2TypeP Qо Тв2 параметр
        //AT35 Qg_2TypeP Qг Тв2 параметр
        //A+36 dt_2TypeP dt Тв2 параметр
        //37 tsw_2TypeP резерв параметр
        //38 ta_2TypeP резерв параметр
        //AT39 Qnt_2TypeHIP BНP Тв2 параметр
        //AT40 Qnt_2TypeP BOC Тв2 параметр
        //+41 G1_2Type G1 Тв2 параметр
        //+42 G2_2Type G2 Тв2 параметр
        //+43 G3_2Type G3 Тв2 параметр
        //A+77 NSPrintTypeM_1 Наличие нештатной ситуации по ТВ1 параметр
        //A+78 NSPrintTypeM_2 Наличие нештатной ситуации по ТВ2 параметр
        //A79 QntNS_1 Длительность НС по параметрам Тв1 параметр
        //A80 QntNS_2 Длительность НС по параметрам Тв2 параметр
        //AT+81 DopInpImpP_Type DI параметр
        //A+82 P3P_Type P3 параметр

        List<dynamic> FilterElements(List<dynamic> activeElements, ValueType type)
        {
            var filterElements = new List<dynamic>();

            byte[] addressList;
            switch (type)
            {
                case ValueType.Current:
                    addressList = new byte[] { 0, 1, 2, 9, 10, 14, 15, 16, 19, 20, 21, 22, 23, 24, 31, 32, 36, 41, 42, 43, 77, 78, 81, 82 };
                    break;
                case ValueType.Total:
                case ValueType.TotalCurrent:
                    addressList = new byte[] { 3, 4, 5, 6, 7, 8, 11, 12, 13, 17, 18, 25, 26, 27, 28, 29, 30, 33, 34, 35, 39, 40, 81 };
                    break;
                case ValueType.Day:
                case ValueType.Hour:
                case ValueType.Month:
                    addressList = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 39, 40, 77, 78, 79, 80, 81, 82 };
                    break;
                default:
                    addressList = new byte[0];
                    break;
            }

            foreach (var activeElement in activeElements)
            {
                foreach (var currentAddress in addressList)
                {
                    if (activeElement.Address == currentAddress)
                    {
                        filterElements.Add(activeElement);
                    }
                }
            }

            return filterElements;
        }
    }
}
