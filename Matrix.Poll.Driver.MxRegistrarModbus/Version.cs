using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        enum DeviceType
        {
            TYPE_ED_SUPPLYMGMT = 0,	// TYPE: управление питанием конечного устройства
            TYPE_COORD_ROUTER,	// TYPE: без управления питанием - роутеры и координаторы
            TYPE_VSEL_TEST,	// TYPE: тест VSEL'ов и Ctrl для проверки платы управления питанием
            TYPE_LIGHT_CONTROL = 6,
            TYPE_PUMP_CONTROL = 8,
            TYPE_CONTROL_UPP = 9,
            TYPE_KURCHATOVA13 = 10,
            TYPE_MINI = 11,
            TYPE_KURCHATOVA13_MASTER = 12,
            TYPE_MX1001R4_P12_01 = 13,
            TYPE_MX1005R4_P16D16I_01 = 14,
            //
            TYPE_STM32FXX = 16,
            TYPE_IC485_03 = 128
        };

        enum DeviceType2
        {
            TYPE_IC485_03 = 0
        }
        

        dynamic MakeNewVersionRequest()
        {
            return MakeRegisterRequest(Register_FlashVerNew, 4);
        }

        dynamic MakeOldVersionRequest()
        {
            return MakeRegisterRequest(Register_FlashVerOld, 2);
        }

        public dynamic ParseVersionResponse(dynamic answer)
        {
            var flashver = ParseRegisterResponse(answer);
            if (!flashver.success) return flashver;
            
            var device = "Без типа";
            flashver.devid = -1;
            flashver.dateTime = 0;
            
            if((flashver.Register[1] > 0) || (flashver.Register[0] > 0))
            {
                if(flashver.Register[1] >= 0x80)
                {
                    flashver.devid = flashver.Register[1];
                    flashver.ver = flashver.Register[0] & 0x0F;
                    flashver.flash = null;// flashver.Register[2] * 4;
                    //flashver.dateTime = 1;
                }
                else
                {
                    flashver.devid = flashver.Register[1] >> 3;   // номер(тип) устройства
                    flashver.ver = flashver.Register[1] & 0x7;  // версия ПО устройства
                    flashver.flash = flashver.Register[0];      // размер конфигурации флеш-памяти
                }

                switch ((DeviceType)flashver.devid)
                {
                    //chip STM32F100C8 with old register set
                    case DeviceType.TYPE_KURCHATOVA13:
                        device = "Кур.13";
                        break;
                    case DeviceType.TYPE_KURCHATOVA13_MASTER:
                        device = "Кур.13М";
                        break;
                    case DeviceType.TYPE_MINI:
                        device = "Мини";
                        break;
                    case DeviceType.TYPE_LIGHT_CONTROL:
                        device = "Управление светом";
                        break; 
                    case DeviceType.TYPE_PUMP_CONTROL:
                        device = "УПП (pump)";
                        break;
                    case DeviceType.TYPE_CONTROL_UPP:
                        device = "Управление УПП";
                        break;
                    //chip STM32F100C8 with new register set
                    case DeviceType.TYPE_MX1001R4_P12_01:
                        device = "MX1001R4-P(12)-01";
                        break;

                    case DeviceType.TYPE_MX1005R4_P16D16I_01:
                        device = "MX1005R4-P16D16I-01";
                        break;

                    //chip STM32F051C8 
                    case DeviceType.TYPE_IC485_03:
                        device = "IC485.03 rev.A";
                        break;

                    //chip STM32F051C8 
                    case DeviceType.TYPE_STM32FXX:
                        device = "Устройство на базе МК семейства STM32F0";
                        break;
                }
            }

            flashver.device = device;

            return flashver;
        }
    }

}
