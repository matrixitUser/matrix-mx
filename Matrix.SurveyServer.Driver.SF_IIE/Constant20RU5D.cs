using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetConstant20RU5D(byte na, byte tube)
        {
            //0x02 или 0x42 (новые)
            return ParseConstant20RU5D(Send(MakeRequest(na, 0x02, new byte[] { tube })));
        }

        private dynamic ParseConstant20RU5D(byte[] bytes)
        {
            dynamic constant = ParseResponse(bytes);
            if (!constant.success) return constant;

            constant.records = new List<dynamic>();

            constant.date = new DateTime(2000 + constant.body[53], constant.body[51], constant.body[52], constant.body[54], constant.body[55], constant.body[56]);

            constant.channel = constant.body[1];

            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Плотность", constant.channel), BitConverter.ToSingle(constant.body, 17), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} CO2, %", constant.channel), BitConverter.ToSingle(constant.body, 21), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} N2, %", constant.channel), BitConverter.ToSingle(constant.body, 25), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Атм. давление, кПа", constant.channel), BitConverter.ToSingle(constant.body, 29), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Отсечка по расходу, сек", constant.channel), BitConverter.ToSingle(constant.body, 33), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Отсечка по частоте, Гц", constant.channel), BitConverter.ToSingle(constant.body, 37), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} А коэфициент преоб. турб., 1/м³", constant.channel), BitConverter.ToSingle(constant.body, 41), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Теплотворная способность, МДж/м³", constant.channel), BitConverter.ToSingle(constant.body, 45), constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Коэффициент масштабирования", constant.channel), constant.body[49], constant.date));
            constant.records.Add(MakeConstRecord(string.Format("Тр.{0} Статус корректирования А", constant.channel), constant.body[50], constant.date));

            return constant;
        }
    }
}
