using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetConstant20RU6D(byte na, byte tube)
        {
            //0x02 или 0x42 (новые)
            return ParseConstant20RU6D(Send(MakeRequest(na, 0x02, new byte[] { tube })));
        }

        private dynamic ParseConstant20RU6D(byte[] bytes)
        {
            dynamic constant = ParseResponse(bytes);
            if (!constant.success) return constant;

            constant.records = new List<dynamic>();

            constant.date = new DateTime(2000 + constant.body[64], constant.body[62], constant.body[63], constant.body[65], constant.body[66], constant.body[67]);

            constant.channel = constant.body[0];

            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Плотность", constant.channel), BitConverter.ToSingle(constant.body, 17), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} CO2, %", constant.channel), BitConverter.ToSingle(constant.body, 21), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} N2, %", constant.channel), BitConverter.ToSingle(constant.body, 25), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Диаметр ИТ, мм", constant.channel), BitConverter.ToSingle(constant.body, 29), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Диаметр СУ, мм", constant.channel), BitConverter.ToSingle(constant.body, 33), constant.date));

            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Атм. давление, кПа", constant.channel), BitConverter.ToSingle(constant.body, 37), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Отсечка по DP", constant.channel), BitConverter.ToSingle(constant.body, 41), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Уровень переключения DP, кПа", constant.channel), BitConverter.ToSingle(constant.body, 57), constant.date));                        
            return constant;
        }
    }
}
