using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private dynamic GetEvents(byte na, int index)
        {
            var resp = Send(MakeRequest(Direction.MasterToSlave, na, 141, BitConverter.GetBytes(index)));
            return ParseEvent(resp);
        }

        private dynamic ParseEvent(byte[] data)
        {
            dynamic events = ParseResponse(data);
            if (!events.success) return events;

            if (events.length == 0)
            {
                events.success = false;
                events.error = "нулевой ответ";
                return events;
            }

            var timeSeconds = BitConverter.ToUInt32(events.body, 0);
            events.date = new DateTime(1997, 01, 01).AddSeconds(timeSeconds);

            var cod = (byte)events.body[4];
            var oldValue = BitConverter.ToSingle(events.body, 5);
            var newValue = BitConverter.ToSingle(events.body, 9);
            string desc = null;

            switch (cod)
            {
                //
                case 0:
                    desc = string.Format("возникла ошибка измерения скорости V");
                    break;
                case 1:
                    desc = string.Format("возникла ошибка P");
                    break;
                case 2:
                    desc = string.Format("возникла ошибка T");
                    break;
                case 3:
                    desc = string.Format("возникла ошибка Q");
                    break;
                //
                case 45:
                    desc = string.Format("рестарт");
                    break;
                case 46:
                    desc = string.Format("сбой программы");
                    break;
                case 47:
                    desc = string.Format("перезапуск программы");
                    break;
                //
                case 50:
                    desc = string.Format("восстановление V");
                    break;
                case 51:
                    desc = string.Format("восстановление P");
                    break;
                case 52:
                    desc = string.Format("восстановление T");
                    break;
                case 53:
                    desc = string.Format("восстановление Q");
                    break;
                //
                case 100:
                    desc = string.Format("выполнен переход на летнее время");
                    break;
                case 101:
                    desc = string.Format("выполнен переход на зимнее время");
                    break;
                case 102:
                    desc = string.Format("обнаружен разряд литиевой батареи до {0} мВ", newValue);
                    break;
                default:
                    break;
            }

            events.records = new List<dynamic>();
            events.records.Add(MakeAbnormalRecord(desc ?? string.Format("ошибка {0} с параметрами {1}=>{2}", cod, oldValue, newValue), 0, events.date));

            return events;
        }
    }
}
