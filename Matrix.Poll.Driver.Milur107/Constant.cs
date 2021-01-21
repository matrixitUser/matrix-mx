using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.Milur107
{
    public partial class Driver
    {
        #region Журнал вкл.- выкл.
        private void GetJournal()
        {
            dynamic dt = Send(MakePackage(0x06, 0x3C, 0xff), 0x06);
            if (!dt.success)
            {
                return;
            }
            int len = dt.Body[1] << 8 | dt.Body[0];
            int count = 0;
            int parity = 0;
            List<dynamic> listTmpJournalOn = new List<dynamic>();
            List<dynamic> listTmpJournalOff = new List<dynamic>();
            while (count < len)
            {
                byte b0 = (byte)(count & 0xFF);
                byte b1 = (byte)(count >> 8);
                byte[] addBytes = new byte[] { b0, b1 };
                dynamic record = new ExpandoObject();
                dt = Send(MakePackage(0x0B, 0x3C, 0xff, addBytes), 0x0B);
                if (!dt.success)
                {
                    return;
                }

                if (parity % 2 == 0)
                {
                    if ((dt.Body[6] >> 3 & 1) == 1)
                    {
                        record.dateOff = new DateTime((int)dt.Body[5] + 2000, (int)dt.Body[4], (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], (int)dt.Body[0]);
                        listTmpJournalOff.Add(record);
                        parity++;
                    }
                }
                else if (parity % 2 != 0)
                {
                    if ((dt.Body[6] >> 4 & 1) == 1)
                    {
                        record.dateOn = new DateTime((int)dt.Body[5] + 2000, (int)dt.Body[4], (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], (int)dt.Body[0]);
                        listTmpJournalOn.Add(record);
                        parity++;
                    }
                }
                count++;
            }
            for (int i = 0; i < listTmpJournalOff.Count(); i++)
            {
                dynamic tmpRecord = new ExpandoObject();
                tmpRecord.dateOff = listTmpJournalOff[i];
                tmpRecord.dateOn = listTmpJournalOn[i];
                listJournalOnOff.Add(tmpRecord);
            }
        }
        #endregion

        #region Серийный номер
        private void GetSerialNumber()
        {
            dynamic dt = Send(MakePackage(0x01, 0x44, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }
            SerialNumber = ASCIIEncoding.ASCII.GetString(dt.Body);
            log($"Серийный номер счетчика: Милур {SerialNumber}");
        }
        #endregion

        #region Модель счетчика
        private void GetModelVersion()
        {
            dynamic dt = Send(MakePackage(0x01, 0x20, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }
            ModelVersion = ASCIIEncoding.ASCII.GetString(dt.Body);
            log($"Модель счетчика: Милур {ModelVersion}");
        }
        #endregion

        #region Токи
        private void GetToque1()
        {
            dynamic dt = Send(MakePackage(0x01, 0x67, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }

            double toque = (dt.Body[1] * 256 + dt.Body[0]) * 0.001;
            log($"Ток на фазе А: {toque}");
        }
        private void GetToque2()
        {
            dynamic dt = Send(MakePackage(0x01, 0x68, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }

            double toque = (dt.Body[1] * 256 + dt.Body[0]) * 0.001;
            log($"Ток на фазе B: {toque}");
        }
        private void GetToque3()
        {
            dynamic dt = Send(MakePackage(0x01, 0x69, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }

            double toque = (dt.Body[1] * 256 + dt.Body[0]) * 0.001;
            log($"Ток на фазе C: {toque}");
        }
        #endregion

        #region Версия прошивика
        private void GetSoftwareVersion()
        {
            dynamic dt = Send(MakePackage(0x01, 0x21, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }
            SoftwareVersion = ASCIIEncoding.ASCII.GetString(dt.Body);
            log($"Версия прошивки: {SoftwareVersion}");
        }
        #endregion

        #region Серийный номер печатного узла
        private string GetSerialNumberPrintNode()
        {
            dynamic dt = Send(MakePackage(0x01, 0x47, 0xff), 0x01);
            if (!dt.success)
            {
                return "---";
            }
            string SerialNumberPrintNode = ASCIIEncoding.ASCII.GetString(dt.Body);
            log($"Серийный номер печатного узла: {SerialNumberPrintNode}");
            return SerialNumberPrintNode;
        }
        #endregion

        #region Цифровой идентификатор
        private string GetIdentificatorPO()
        {
            dynamic dt = Send(MakePackage(0x01, 0x3E, 0xff), 0x01);
            if (!dt.success)
            {
                return "---";
            }
            string identificatorPO = dt.Body[1].ToString("X") + dt.Body[0].ToString("X");
            log($"Цифровой идентификатор: {identificatorPO}");
            return identificatorPO;
        }
        #endregion

        #region Версия метрологически значимой части ПО
        private string GetVersionMetrolPO()
        {
            dynamic dt = Send(MakePackage(0x01, 0x53, 0xff), 0x01);
            if (!dt.success)
            {
                return "---";
            }
            string metrolPO = ASCIIEncoding.ASCII.GetString(dt.Body);
            log($"Версия метрологически значимой части ПО: {metrolPO}");
            return metrolPO;
        }
        #endregion

        #region Напряжение батарейки
        private double GetVoltageBattary()
        {
            dynamic dt = Send(MakePackage(0x01, 0x39, 0xff), 0x01);
            if (!dt.success)
            {
                return -1;
            }
            double voltageBattary = (double)(dt.Body[1] << 8 | dt.Body[0]) / 1000;
            log($"Напряжение резервного питания: {voltageBattary}");
            return voltageBattary;
        }
        #endregion

        #region Максимальное число тарифов
        private uint GetMaxNumberOfTarifs()
        {
            dynamic dt = Send(MakePackage(0x01, 0x13, 0xff), 0x01);
            if (!dt.success)
            {
                return 0;
            }
            maxNumberOfTarifs = (uint)dt.Body[0] + 1;
            log($"Максимальное число тарифов: {maxNumberOfTarifs}");
            return maxNumberOfTarifs;
        }
        #endregion

        #region Фактор мощности
        private double GetFactorPower()
        {
            dynamic dt = Send(MakePackage(0x01, 0x50, 0xff), 0x01);
            if (!dt.success)
            {
                return 0;
            }
            double factorPower = (double)(dt.Body[1] << 8 | dt.Body[0]) / 1000;
            log($"Фактор мощности: {factorPower}");
            return factorPower;
        }
        #endregion

        #region Текущий тариф
        private uint GetCurrentTarif()
        {
            dynamic dt = Send(MakePackage(0x01, 0x0A, 0xff), 0x01);
            if (!dt.success)
            {
                return 0;
            }
            currentTarif = (uint)dt.Body[0];
            log($"Текущий тариф: {currentTarif}");
            return currentTarif;
        }
        #endregion

        #region Получение состояния включения-выклычения нагрузки
        private void GetElectricalLoad()
        {
            dynamic dt = Send(MakePackage(0x01, 0x54, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }
            uint tmpElectricalLoad = (uint)dt.Body[0];
            if (tmpElectricalLoad == 1)
            {
                electricalLoadSoft = false;
                log("Нагрузка выключена");
            }
            else
            {
                electricalLoadSoft = true;
                log("Нагрузка включена");
            }
        }
        #endregion

        #region Установка состояния вкл-выкл нагрузки
        private void SetElectricalLoad(byte OnOff)
        {
            dynamic dt = Send(MakePackage(0x02, 0x54, OnOff), 0x02);
            if (!dt.success)
            {
                return;
            }
        }
        #endregion

        #region Защитное отключение нагрузки
        private void GetDeffenderLoad()
        {
            dynamic dt = Send(MakePackage(0x01, 0x38, 0xff), 0x01);
            if (!dt.success)
            {
                return;
            }
            float minVoltage = (float)(dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]);
            float maxVoltage = (float)(dt.Body[5] << 16 | dt.Body[4] << 8 | dt.Body[3]);
            float maxPower = (float)(dt.Body[9] << 24 | dt.Body[8] << 16 | dt.Body[7] << 8 | dt.Body[6]) / 1000;
            float ctrlTime = (float)dt.Body[10];
            //float paramCtrl;
            //float causeOff;
            log($"мин знач напряжения: {minVoltage}, в");
            log($"макс знач напряжения: {maxVoltage}, в");
            log($"макс знач мощности: {maxPower}, Вт");
            log($"время контроля: {ctrlTime}, сек");
            //log($"мин знач напряжения: {paramCtrl}");
            //log($"мин знач напряжения: {causeOff}");
        }
        #endregion

        #region Установка защитного отключения нагрузки
        #endregion

        dynamic GetConstant(DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            //GetDeffenderLoad();

            //GetElectricalLoad();

            //if (electricalLoad == true && electricalLoadSoft == false) SetElectricalLoad(0x00);
            //else if(electricalLoad == false && electricalLoadSoft == true) SetElectricalLoad(0x01);

            GetToque1();
            GetToque2();
            GetToque3();

            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Версия прошивки", SoftwareVersion, date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Модель счетчика: Милур", ModelVersion, date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Серийный номер счетчика: Милур", SerialNumber, date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Максимальное число тарифов", maxNumberOfTarifs, date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Фактор мощности", GetFactorPower(), date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            

            records.Add(MakeConstRecord("Текущий тариф", GetCurrentTarif(), date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Напряжение резервного питания", GetVoltageBattary(), date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Версия метрологически значимой части ПО", GetVersionMetrolPO(), date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Цифровой идентификатор", GetIdentificatorPO(), date));
            if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

            records.Add(MakeConstRecord("Серийный номер печатного узла", GetSerialNumberPrintNode(), date));

            constant.records = records;

            return constant;
        }
    }
}
