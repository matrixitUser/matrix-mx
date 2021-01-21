using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetConstants(byte na, string version, DateTime date, byte task)
        {
            var length = 384;
            if (version.Contains("Z") || version.Contains("X"))
            {
                length = 256;
            }
            var constsLen = length + 6 + 3 + 1 + 1;
            return ParseConstants(GetBlocks(na, 0x94, 1, 1, constsLen), version, date, task);
        }

        private dynamic ParseConstants(byte[] bytes, string version, DateTime date, byte task)
        {
            dynamic constansts = new ExpandoObject();
            constansts.success = true;

            constansts.records = new List<dynamic>();

            if (bytes == null || !bytes.Any())
            {
                constansts.success = false;
                constansts.error = "нет данных для разбора";
                return constansts;
            }

            if (version.Contains("K"))
            {
                constansts.contractHour = 0;//(byte)(BitConverter.ToSingle(bytes, 22∙6 + 2) / 2);

                var map = GetTaskMap(task);

                foreach (var m in map)
                {
                    var x = BitConverter.ToSingle(bytes, (m.Key - 1)*6 + 2) / 2;
                    if (m.Value == "tr - расчетный час")
                    {
                        constansts.contractHour = (byte)x;
                    }
                    constansts.records.Add(MakeConstRecord(m.Value, x, date));
                }

                //constansts.records.Add(MakeConstRecord("контрактный час", constansts.contractHour, date));
                //constansts.records.Add(MakeConstRecord("Ron-плотн.при н.у.,кг/м³", BitConverter.ToSingle(bytes, 3∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("fiw-относительная влажность при р.у., %", BitConverter.ToSingle(bytes, 6∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Метан (CH₄)", BitConverter.ToSingle(bytes, 23∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Этан (C₂H₆)", BitConverter.ToSingle(bytes, 24∙6 + 2) / 2, date));

                //constansts.records.Add(MakeConstRecord("Пропан (C₃H₈)", BitConverter.ToSingle(bytes, 25∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("н-Бутан (n-C₄H₁₀)", BitConverter.ToSingle(bytes, 26∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("и-Бутан (i-C₄H₁₀)", BitConverter.ToSingle(bytes, 27∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Азот (N₂)", BitConverter.ToSingle(bytes, 5∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Диоксид углерода (CO₂)", BitConverter.ToSingle(bytes, 4∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Сероводород (H₂S)", BitConverter.ToSingle(bytes, 28∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Природный газ", BitConverter.ToSingle(bytes, 8∙6 + 2) / 2, date));

                //constansts.records.Add(MakeConstRecord("SW-подключить догов.парам.(1-да, 0-нет).", BitConverter.ToSingle(bytes, 16∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Qo1-расход объмный, м³/час", BitConverter.ToSingle(bytes, 12∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("QO2-расход объмный, м³/час", BitConverter.ToSingle(bytes, 13∙6 + 2) / 2, date));

                //constansts.records.Add(MakeConstRecord("Рir-давление,", BitConverter.ToSingle(bytes, 20∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("tn-температура, °С", BitConverter.ToSingle(bytes, 21∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("Pбар-Барометр. давление, мм рт.ст.", BitConverter.ToSingle(bytes, 11∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("QO2-расход объмный, м³/час", BitConverter.ToSingle(bytes, 13∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("P0", BitConverter.ToSingle(bytes, 49∙6 + 2) / 2, date));
                //constansts.records.Add(MakeConstRecord("T0", BitConverter.ToSingle(bytes, 50∙6 + 2) / 2, date));
            }
            else
            {
                constansts.contractHour = (byte)(BitConverter.ToSingle(bytes, 23*4) / 2);
            }



            return constansts;
        }
    }
}
