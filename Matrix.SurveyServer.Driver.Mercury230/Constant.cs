using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        private dynamic GetConst(DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            var records = new List<dynamic>();
            ///Чтение серийного номера счетчика и даты выпуска.
            ///4 байта серийного номера и три байта кода даты выпуска в последовательности: число, месяц, год
            var sn = Send(MakeParametersRequest(0x00));
            if (!sn.success)
            {
                return sn;
            }
    
            var snData = sn.Body;

            //parse
            var serialNumber = snData[0] * 1000000 + snData[1] * 10000 + snData[2] * 100 + snData[3]; // Helper.ToInt32(snData, 1);
            var day = snData[4];
            var month = snData[5];
            var year = 2000 + snData[6];
            var productionDate = new DateTime(year, month, day);
            records.Add(MakeConstRecord("Серийный номер", serialNumber.ToString(), date));
            records.Add(MakeConstRecord("Дата изготовления", productionDate.ToString(), date));
            constant.records = records;
            log(string.Format("Заводской номер счетчика {0}. Изготовлен:{1}", serialNumber.ToString(), productionDate.ToString()));
            return constant;
        }

        dynamic GetConstant(DateTime Date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            dynamic getConstant = GetConst(Date);
            if (!getConstant.success)
            {
                return getConstant;
            }

            records.AddRange(getConstant.records);

            ///Чтение сетевого адреса.
            ///2 двоичных байта (первый=0).

            var naData = Send(MakeParametersRequest(0x05));
            if (!naData.success) return naData;

            var na = naData.Body[1];

            records.Add(MakeConstRecord("Сетевой адрес", na.ToString(), Date));

            var version = ParseVersionResponse(Send(MakeParametersRequest(0x03)));
            if (!version.success) return version;

            var variant = ParseVariantResponse(Send(MakeParametersRequest(0x12)));
            if (!variant.success) return variant;

            var trans = ParseTransformationResponse(Send(MakeParametersRequest(0x02)));
            if (!trans.success) return trans;

            //records.Add(MakeConstRecord("Kн", trans.Kn.ToString(), Date));
            //records.Add(MakeConstRecord("Kт", trans.Kt.ToString(), Date));

            foreach (var data in records)
            {
                log(string.Format("{0} = {1}", data.s1, data.s2));
            }

            constant.records = records;
            constant.version = version.Version;
            constant.variant = variant;
            return constant;
        }
    }
}
