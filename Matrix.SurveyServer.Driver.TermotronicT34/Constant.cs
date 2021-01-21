using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        dynamic GetConstant(DateTime dt)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;
            DateTime Date = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
            var records = new List<dynamic>();
           
            var sn = Send(MakeBaseRequest0X48(0, 14, 0, 0, 0, 0)); //адрес от нуля
            if (!sn.success) return sn;
            IEnumerable<byte> snData = (IEnumerable<byte>)sn.Body;

            string type = (snData.ToArray()[0] == 0x17 && snData.ToArray()[1] == 0x02) ? "ТВ7" : "тип устройства не определен";

            string programVersion = string.Format("{0}", string.Join(".", snData.Skip(2).Take(2).ToArray().Select(b => b.ToString())));

            double version = snData.ToArray()[2] + snData.ToArray()[3] / 10;
            string aparatVersion = string.Format("{0}", string.Join(".", snData.Skip(4).Take(2).ToArray().Select(b => b.ToString())));
            log("Программная версия: " + programVersion + "; Аппаратная версия: " + aparatVersion, level: 1);

            string KCPO = string.Format("{0}", string.Join("", snData.Skip(6).Take(2).ToArray().Select(b => b.ToString("X2"))));
            log(string.Format("Контрольная сумма ПО: {0}", KCPO), level: 1);

            byte byteModel = snData.ToArray()[9];
            double model = 0;
            switch (byteModel)
            {
                case 0:
                    model = 1;
                    break;
                case 1:
                    model = 2;
                    break;
                case 2:
                    model = 3;
                    break;
                case 3:
                    model = 4;
                    break;
                case 4:
                    model = 4.1;
                    break;
                case 5:
                    model = 5;
                    break;
            }
            log(string.Format("Модель: {0}-{1}", type, model), level: 1);

            int serialNumber = BitConverter.ToInt32(Helper.ReverseRegister(snData.Skip(10).Take(4)).Reverse().ToArray(), 0);
            log(string.Format("Серийный номер: {0}", serialNumber), level: 1);

            IEnumerable<byte> respBody;
            if (version < 2.2)
                respBody = (IEnumerable<byte>)Send(MakeBaseRequest0X48(2669, 4, 0, 0, 0, 0)).Body;
            else
                respBody = (IEnumerable<byte>)Send(MakeBaseRequest0X48(2672, 4, 0, 0, 0, 0)).Body;

            string KCH = string.Format("{0}", string.Join("", respBody.Take(2).ToArray().Select(b => b.ToString("X2"))));
            log(string.Format("Контрольная сумма настроек ПО: {0}", KCH), level: 1);
            
            records.Add(MakeConstRecord("Тип устройства", type, Date));
            records.Add(MakeConstRecord("Программная версия", programVersion, Date));
            records.Add(MakeConstRecord("Аппаратная версия", aparatVersion, Date));
            records.Add(MakeConstRecord("Контрольная сумма ПО", KCPO, Date));
            records.Add(MakeConstRecord("Модель", model.ToString(), Date));
            records.Add(MakeConstRecord("Серийный номер", serialNumber.ToString(), Date));
            records.Add(MakeConstRecord("КСН", KCH, Date));
            constant.records = records;
            constant.version = programVersion;
            return constant;
        }
    }
}
