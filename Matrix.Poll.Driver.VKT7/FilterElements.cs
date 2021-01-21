using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
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

        List<dynamic> FilterElements(List<dynamic> activeElements, ValueType type, IEnumerable<int> channels)
        {
            var filterElements = new List<dynamic>();

            List<byte> addressList;
            switch (type)
            {
                case ValueType.Current:
                    addressList = new List<byte>() { 15, 16, 81, 82 };
                    if (channels.Contains(1))
                    {
                        addressList.AddRange(new byte[] { 0, 1, 2, 9, 10, 14, 19, 20, 21, 77 });
                    }
                    if (channels.Contains(2))
                    {
                        addressList.AddRange(new byte[] { 22, 23, 24, 31, 32, 36, 41, 42, 43, 78 });
                    }
                    break;
                case ValueType.Total:
                case ValueType.TotalCurrent:
                    addressList = new List<byte>() { 81 };
                    if (channels.Contains(1))
                    {
                        addressList.AddRange(new byte[] { 3, 4, 5, 6, 7, 8, 11, 12, 13, 17, 18 });
                    }
                    if (channels.Contains(2))
                    {
                        addressList.AddRange(new byte[] { 25, 26, 27, 28, 29, 30, 33, 34, 35, 39, 40 });
                    }
                    break;
                case ValueType.Day:
                case ValueType.Hour:
                case ValueType.Month:
                    addressList = new List<byte>() { 15, 16, 81, 82 };
                    if (channels.Contains(1))
                    {
                        addressList.AddRange(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 17, 18, 77, 79 });
                    }
                    if (channels.Contains(2))
                    {
                        addressList.AddRange(new byte[] { 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 39, 40, 78, 80 });
                    }
                    break;
                default:
                    addressList = new List<byte>();
                    break;
            }

            //addressList.Sort();

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

            log($"type={type} ch=[{string.Join(",", channels.Select(x => $"{x}"))}]: [{string.Join(",", activeElements.Select(a => $"{a.Address}"))}] & [{string.Join(",", addressList.Select(x => $"{x}"))}] => [{string.Join(",", filterElements.Select(x => $"{x.Address}({x.Length})"))}]", level: 3);
            return filterElements;
        }
    }
}
