using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        private dynamic GetConstant(DateTime currentdt, dynamic flashver)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;

            UInt16 devid = (UInt16)flashver.devid;

            var text = "";

            var records = new List<dynamic>();

            #region chipid
            {
                var chipid = ParseChipidResponse(Send(MakeChipidRequest(devid)));
                if (chipid.success == true && chipid.enabled == true)
                {
                    records.Add(MakeConstRecord("ChipID", chipid.text, currentdt));
                }
            }
            #endregion

            records.Add(MakeConstRecord("Модель", $"{flashver.device} v.{flashver.ver}{ (flashver.flash == null ? "" : $".{flashver.flash}")}", currentdt));

            if (GetRegisterSet(devid).name == "new")
            {
                var config = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).NA, 9)));
                if (!config.success) return config;

                var constna = string.Format("{0}", config.Register[0]);
                var constwm = config.Register[1] == 0 ? "сервис" : "работа";
                text = string.Format("сет.адрес - {0}; режим работы - {1}", constna, constwm);

                records.Add(MakeConstRecord("Сетевой адрес", constna, currentdt));
                records.Add(MakeConstRecord("Режим работы", constwm, currentdt));

                records.Add(MakeConstRecord("RS485 - Скорость обмена", BitConverter.ToUInt32(config.Register, 2).ToString(), currentdt));
                records.Add(MakeConstRecord("RS485 - Длина слова", config.Register[6] == 0 ? "8 бит" : "9 бит", currentdt));
                records.Add(MakeConstRecord("RS485 - Стоповые биты",
                    config.Register[7] == 0x00 ? "1" :
                    (config.Register[7] == 0x10 ? "0.5" :
                    (config.Register[7] == 0x20 ? "2" : "1.5")),
                    currentdt));
                records.Add(MakeConstRecord("RS485 - Бит чётности",
                    config.Register[8] == 0x00 ? "нет" :
                    (config.Register[8] == 0x40 ? "чёт" : "нечёт"),
                    currentdt));
            }
            else
            {
                var config = ParseConfigResponse(Send(MakeConfigRequest()));
                if (!config.success) return config;

                log(string.Format("ответ на запрос чтения конфигурации: операция {0} с результатом: {1} (код {2})", config.SubFunction, config.Result == 0 ? "Успех" : "Неудача", config.Result));

                var cfg = new FlashConfig(config.Config);
                log(cfg.ToString());

                records.Add(MakeConstRecord("NetworkAddress", string.Format("{0}", cfg.u8NetworkAddress), currentdt));
                records.Add(MakeConstRecord("USART Baudrate", string.Format("{0}", cfg.u32BaudRate), currentdt));
                records.Add(MakeConstRecord("USART Wordlength", cfg.eWordLen == FlashConfig.USART_WordLength.USART_WordLength_8b ? "8" : "9", currentdt));
                records.Add(MakeConstRecord("USART Parity", cfg.eParity == FlashConfig.USART_Parity.USART_Parity_No ? "N" : cfg.eParity == FlashConfig.USART_Parity.USART_Parity_Even ? "E" : "O", currentdt));
                records.Add(MakeConstRecord("USART StopBits", (cfg.eStopBits == FlashConfig.USART_StopBits.USART_StopBits_0_5 ? "0.5" : cfg.eStopBits == FlashConfig.USART_StopBits.USART_StopBits_1 ? "1" :
                        cfg.eStopBits == FlashConfig.USART_StopBits.USART_StopBits_1_5 ? "1.5" : "2"), currentdt));

            }

            constant.records = records;
            constant.text = text;
            return constant;
        }


        private dynamic GetConstant4(DateTime currentdt, MessageConfig mc, MessageConfig2 mc2, UInt32[] chipid, string device, int ver)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;
            
            //

            var text = "";
            var records = new List<dynamic>();

            records.Add(MakeConstRecord("ChipID", $"{chipid[0]:X8} {chipid[1]:X8} {chipid[2]:X8}", currentdt));
            records.Add(MakeConstRecord("Модель", $"{device} v.{ver}", currentdt));

            var constna = $"{mc.NA}";
            var constwm = $"{(mc.mode == 0 ? "сервис" : "работа")}";
            records.Add(MakeConstRecord("Сетевой адрес", constna, currentdt));
            records.Add(MakeConstRecord("Режим работы", constwm, currentdt));
            text = string.Format("сет.адрес - {0}; режим работы - {1}", constna, constwm);

            records.Add(MakeConstRecord("Дата изготовления",
                mc2.releaseDt == DateTime.MinValue? "нет данных" : $"{mc2.releaseDt:dd.MM.yyyy HH:mm}",
                currentdt));
            records.Add(MakeConstRecord("RS485 - Скорость обмена", $"{mc.uart1Baud}", currentdt));
            records.Add(MakeConstRecord("RS485 - Длина слова", mc.uart1Wl == 0 ? "8 бит" : "9 бит", currentdt));
            records.Add(MakeConstRecord("RS485 - Стоповые биты",
                mc.uart1Sb == 0x00 ? "1" :
                (mc.uart1Sb == 0x10 ? "0.5" :
                (mc.uart1Sb == 0x20 ? "2" : "1.5")),
                currentdt));
            records.Add(MakeConstRecord("RS485 - Бит чётности",
                mc.uart1Par == 0x00 ? "нет" :
                (mc.uart1Par == 0x40 ? "чёт" : "нечёт"),
                currentdt));

            int i = 0;
            foreach(var cfg in mc.cntConfig)
            {
                records.Add(MakeConstRecord($"Канал {++i} - параметр",
                    cfg.Param,
                    currentdt));
            }
            i = 0;
            foreach (var rDt in mc2.cntReleaseDt)
            {
                records.Add(MakeConstRecord($"Канал {++i} - дата пусконаладки",
                    rDt == DateTime.MinValue? "не запущен" : $"{rDt:dd.MM.yyyy HH:mm}",
                    currentdt));
            }

            constant.records = records;
            constant.text = text;
            return constant;
        }
    }
}
