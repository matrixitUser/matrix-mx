using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        byte[] MakeRequestEnergy(byte numberArray, byte DayOrMonth, byte Tarif)
        {
            var Data = new List<byte>();
            Data.Add(0x0A);//код запроса (чтение параметров)		
            Data.Add(numberArray);
            Data.Add(DayOrMonth);
            Data.Add(Tarif);  //0-сумма, далее 0x01 - первый тариф и т.д
            Data.Add(0xFF); //все виды энергии, чтоб не путаться A+,A-,Q+,Q-,R1,R2,R3,R4
            Data.Add(0x00); //резервировано
            return MakeBaseRequest(Data);
        }
        byte[] MakeRequestEnergyShort(byte numberArray_DayOrMonth, byte Tarif)
        {
            var Data = new List<byte>();
            Data.Add(0x05);//код запроса (чтение параметров)		
            Data.Add(numberArray_DayOrMonth);
            Data.Add(Tarif);  //0-сумма, далее 0x01 - первый тариф и т.д
            return MakeBaseRequest(Data);
        }
        byte[] MakeRequestPower(byte RWRI)
        {
            var Data = new List<byte>();
            Data.Add(0x08);//код запроса (чтение параметров)		
            Data.Add(0x11);
            Data.Add(RWRI);  //0-сумма, далее 0x01 - первый тариф и т.д
            return MakeBaseRequest(Data);
        }
    }
}
