//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    public class Archive
//    {
//        private List<Data> datas;
//        public List<Data> Datas
//        {
//            get { return datas; }
//        }

//        private Archive() { }

//        public static Archive Parse(Driver2.FunCode archiveType, byte[] data, int dataOffset)
//        {
//            Archive result = null;

//            if (data == null) return null;
//            switch (archiveType)
//            {
//                case Driver2.FunCode.ReadHourlyHistory:
//                    result = new Archive { datas = new List<Data>() };
//                    if (data.Length >= 33)
//                    {
//                        DateTime current = ParseDateTime(data, dataOffset + 8, 5);
//                        result.datas.Add(new Data("Volume", MeasuringUnitType.m3, current,
//                                                  ConvertHelper.BinDecToInt32(data, dataOffset + 13, true))); //14*
//                        result.datas.Add(new Data("Energy", MeasuringUnitType.MDj, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 17)));
//                        result.datas.Add(new Data("VolumeConsumptionNormal", MeasuringUnitType.m3_h, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 21)));
//                        result.datas.Add(new Data("Pressure", MeasuringUnitType.kPa, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 25)));
//                        result.datas.Add(new Data("Temperature", MeasuringUnitType.C, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 29)));
//                        result.datas.Add(new Data("VolumeNormal", MeasuringUnitType.m3, current,
//                                                  ConvertHelper.BinDecToInt32(data, dataOffset + 33))); //14
//                    }
//                    break;
//                case Driver2.FunCode.ReadDailyHistory:
//                    result = new Archive { datas = new List<Data>() };
//                    if (data.Length >= 31)
//                    {
//                        DateTime current = ParseDateTime(data, dataOffset + 8, 3);
//                        result.datas.Add(new Data("Volume", MeasuringUnitType.m3, current,
//                                                  ConvertHelper.BinDecToInt32(data, dataOffset + 11, true)));
//                        result.datas.Add(new Data("Energy", MeasuringUnitType.MDj, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 15)));
//                        result.datas.Add(new Data("VolumeConsumptionNormal", MeasuringUnitType.m3_h, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 19)));
//                        result.datas.Add(new Data("Pressure", MeasuringUnitType.kPa, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 23)));
//                        result.datas.Add(new Data("Temperature", MeasuringUnitType.C, current,
//                                                  BitConverter.ToSingle(data, dataOffset + 27)));
//                        result.datas.Add(new Data("VolumeNormal", MeasuringUnitType.m3, current,
//                                                  ConvertHelper.BinDecToInt32(data, dataOffset + 31)));
//                    }
//                    break;
//            }

//            return result;
//        }

//        public static DateTime ParseDateTime(byte[] data, int dateTimeOffset, int dateLength)
//        {
//            var result = DateTime.MinValue;

//            if (data != null && dateLength >= 3 && dateLength <= 6 && data.Count() >= dateTimeOffset + dateLength)
//            {
//                var dateTimeString = string.Format("{0}.{1}.{2} {3}:{4}:{5}",
//                                                                data[dateTimeOffset + 1], //day
//                                                                data[dateTimeOffset + 0], //mon
//                                                                data[dateTimeOffset + 2], //yr
//                                                                dateLength > 3 ? data[dateTimeOffset + 3].ToString(CultureInfo.InvariantCulture) : "00", //hr
//                                                                dateLength > 4 ? data[dateTimeOffset + 4].ToString(CultureInfo.InvariantCulture) : "00", //min
//                                                                dateLength > 5 ? data[dateTimeOffset + 5].ToString(CultureInfo.InvariantCulture) : "00" //sec
//                    );
//                DateTime.TryParse(dateTimeString, out result);
//            }

//            return result;
//        }
//    }
//}
