//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class EventResponse : Response
//    {
//        public DateTime Event { get; private set; }
//        public AbnormalEvents Events { get; private set; }

//        public EventResponse(byte[] data)
//            : base(data)
//        {
//            //Events = new List<AbnormalEvents>();
//            Event = DateTime.MinValue;

//            if (Length != 0)
//            {
//                var timeSeconds = BitConverter.ToUInt32(Body, 0);
//                Event = new DateTime(1997, 01, 01).AddSeconds(timeSeconds);

//                var cod = Body[4];
//                var oldValue = BitConverter.ToSingle(Body, 5);
//                var newValue = BitConverter.ToSingle(Body, 9);
//                string desc = null;

//                switch (cod)
//                {
//                    //
//                    case 0:
//                        desc = string.Format("возникла ошибка измерения скорости V");
//                        break;
//                    case 1:
//                        desc = string.Format("возникла ошибка P");
//                        break;
//                    case 2:
//                        desc = string.Format("возникла ошибка T");
//                        break;
//                    case 3:
//                        desc = string.Format("возникла ошибка Q");
//                        break;
//                    //
//                    case 45:
//                        desc = string.Format("рестарт");
//                        break;
//                    case 46:
//                        desc = string.Format("сбой программы");
//                        break;
//                    case 47:
//                        desc = string.Format("перезапуск программы");
//                        break;
//                    //
//                    case 50:
//                        desc = string.Format("восстановление V");
//                        break;
//                    case 51:
//                        desc = string.Format("восстановление P");
//                        break;
//                    case 52:
//                        desc = string.Format("восстановление T");
//                        break;
//                    case 53:
//                        desc = string.Format("восстановление Q");
//                        break;
//                    //
//                    case 100:
//                        desc = string.Format("выполнен переход на летнее время");
//                        break;
//                    case 101:
//                        desc = string.Format("выполнен переход на зимнее время");
//                        break;
//                    case 102:
//                        desc = string.Format("обнаружен разряд литиевой батареи до {0} мВ", newValue);
//                        break;
//                }

//                Events = new AbnormalEvents { DateTime = Event, Description = (desc ?? string.Format("ошибка {0} с параметрами {1}=>{2}", cod, oldValue, newValue)) };


//            }
//        }
//    }
//}
