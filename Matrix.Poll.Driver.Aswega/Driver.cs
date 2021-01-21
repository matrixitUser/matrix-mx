using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

//Device SA94/3; deviceAddress = 27384;
namespace Matrix.SurveyServer.Driver.SA94
{
    public class Driver
    {
        public struct Status
        {
            public bool b6_isQ2ComputeByT3;
            public bool b5_isNotSA94;
            public bool b4_isModeCount;
            public bool b3_isSA94;
            public bool b2_isTwoChannels;
            public bool b1_SA94_1_isComputeT2Programmed;
            public bool b1_SA94_2_2M_isT3Measured;
            public bool b0_SA94_1_2M_isPrn1OnBackTube;
            public bool b0_SA94_2_isComputeT3Programmed;
        }

        public struct NextAddr
        {
            public UInt16 hour;
            public UInt16 day;
            public UInt16 abnormal;
        }

        public struct DeviceResponse
        {
            public byte[] Body;
        }

        public struct ParsedParameter
        {
            public string parameter;
            public string unit;
            public DateTime date;
            public double value;
        }

        public byte[] MakeDeviceSelectRequest(UInt32 deviceId)
        {
            return new byte[]
            {
            (byte)(0xC0 | ((deviceId >> 14) & 0x3F)),
            (byte)((deviceId >> 7) & 0x7F),
            (byte)(deviceId & 0x7F)
            };
        }

        public byte[] MakeDeviceUnselectRequest()
        {
            return new byte[] { 0xFF };
        }

        public byte[] MakeReadCurrentParametersRequest(byte parameterN)
        {
            return new byte[] { (byte)(0x80 | (parameterN & 0x0F)) };
        }

        public byte[] MakeReadStatisticsRequest(byte S, UInt16 a)
        {
            return new byte[] {
                (byte)(0xA0 | (S & 0x7)),
                (byte)((a >> 7) & 0x7F)
            };
        }

        public byte[] MakeReadStatisticsExtRequest(byte S, UInt16 a)
        {
            return new byte[] {
                0xB7,
                (byte)(0x40 | ((S & 0x3) << 3) | ((a >> 13) & 0x07)),
                (byte)((a >> 7) & 0x3F)
            };
        }

        public byte[] MakeReadNextAddrInStatisticsExtRequest()
        {
            return new byte[] { 0xB8 };
        }

        public byte[] MakeTimeCorrectionRequest(byte hour, byte minute, byte second)
        {
            return new byte[] {
                0xB5,
                0x78,
                DriverHelper.ByteToBCD(hour),
                DriverHelper.ByteToBCD(minute),
                DriverHelper.ByteToBCD(second)
            };
        }

        public byte[] MakeTimeCorrectionRequest(DateTime date)
        {
            return MakeTimeCorrectionRequest((byte)date.Hour, (byte)date.Minute, (byte)date.Second);
        }

        public struct CurrentParameter
        {
            public byte P;
            public string name;
            public string description;
            public string unit;
            public VersionMask vmask;
        }

        public enum VersionMask
        {
            SA94_All,
            No_SA94_1,
            No_SA94_100_200_300_M100_M200_M300
        }

        public struct Version
        {
            public VersionHardware verHw;
            public VersionSoftware verSw;
            public string verText;
            public bool isExtended;
            public bool isM;
        }

        public enum VersionHardware
        {
            SA94_1,
            SA94_2,
            SA94_2M
        }

        public enum VersionSoftware
        {
            ver100,
            verM100,
            ver101,
            verM101,
            ver200,
            ver201,
            verMTE1,
            ver300,
            verM300,
            ver301,
            verM301
        }

        public enum SegmentOldStats
        {
            Service1,
            Service2,
            Hourly1,
            Hourly2,
            Daily1,
            Daily2,
            Abnormal1,
            Abnormal2
        }

        public Dictionary<byte, CurrentParameter> currentParameter = new Dictionary<byte, CurrentParameter>()
        {
            { (byte)0, new CurrentParameter { P=0, name="Q1", description = "расход теплоносителя в подающем или обратном трубопроводе", unit = "м3/с", vmask = VersionMask.SA94_All } },
            { (byte)1, new CurrentParameter { P=1, name="Q2", description = "расход теплоносителя в обратном или третьем трубопроводе", unit = "м3/с", vmask = VersionMask.No_SA94_1 } },
            { (byte)2, new CurrentParameter { P=2, name="T1", description = "температура теплоносителя в подающем трубопроводе", unit = "°C", vmask = VersionMask.SA94_All } },
            { (byte)3, new CurrentParameter { P=3, name="T2", description = "температура теплоносителя в обратном трубопроводе", unit = "°C", vmask = VersionMask.SA94_All } },
            { (byte)4, new CurrentParameter { P=4, name="T3", description = "температура теплоносителя в третьем трубопроводе (при его наличии)", unit = "°C", vmask = VersionMask.No_SA94_1 } },
            { (byte)5, new CurrentParameter { P=5, name="dT", description = "разность температур теплоносителя в трубопроводах", unit = "°C", vmask = VersionMask.SA94_All } },
            { (byte)6, new CurrentParameter { P=6, name="P", description = "потребляемая тепловая мощность", unit = "кВт", vmask = VersionMask.SA94_All } },
            { (byte)7, new CurrentParameter { P=7, name="E", description = "количество теплоты", unit = "MВт*ч", vmask = VersionMask.SA94_All } },
            { (byte)8, new CurrentParameter { P=8, name="V1", description = "объем теплоносителя, прошедшая через первый преобразователь расхода", unit = "м3", vmask = VersionMask.SA94_All } },
            { (byte)9, new CurrentParameter { P=9, name="V2", description = "объем теплоносителя, прошедшая через второй преобразователь расхода", unit = "м3", vmask = VersionMask.No_SA94_1 } },
            { (byte)10, new CurrentParameter { P=10, name="time", description = "Суточное время", unit = "", vmask = VersionMask.SA94_All } },
            { (byte)11, new CurrentParameter { P=11, name="date", description = "Дата", unit = "", vmask = VersionMask.SA94_All } },
            { (byte)12, new CurrentParameter { P=12, name="tw", description = "Время работы теплосчетчика в режиме \"Работа\" и \"Счет\"", unit = "с", vmask = VersionMask.SA94_All } },
            //{ (byte)13, new CurrentParameter { P=13, name="undefined", description = "", unit = "" } },
            { (byte)14, new CurrentParameter { P=14, name="p1", description = "измерение давления в первом трубопроводе", unit = "МПа", vmask = VersionMask.No_SA94_100_200_300_M100_M200_M300 } },
            { (byte)15, new CurrentParameter { P=15, name="p2", description = "измерение давления во втором трубопроводе", unit = "МПа", vmask = VersionMask.No_SA94_100_200_300_M100_M200_M300 } }
        };

        //1 м3/ч = 3600 м3/с
        //1 Гкал = 1,163 МВтч
        //1 Мкал/ч = 1,163кВт



        public Status ParseDeviceSelectResponse(DeviceResponse answer, VersionHardware verHw)
        {
            if (answer.Body.Length != 1) throw new Exception("ответ не распознан (несовпадение по длине)");
            byte b = answer.Body[0];
            Status s = new Status();

            s.b6_isQ2ComputeByT3 = (b & 0x40) > 0;
            s.b5_isNotSA94 = (b & 0x20) > 0;
            s.b4_isModeCount = (b & 0x10) > 0;
            s.b3_isSA94 = (b & 0x08) > 0;
            s.b2_isTwoChannels = (b & 0x04) > 0;

            if (s.b5_isNotSA94 || !s.b3_isSA94) throw new Exception("SA-94 не обнаружен");

            switch (verHw)
            {
                case VersionHardware.SA94_1:
                    s.b1_SA94_1_isComputeT2Programmed = (b & 0x02) > 0;
                    s.b0_SA94_1_2M_isPrn1OnBackTube = (b & 0x01) > 0;
                    break;
                case VersionHardware.SA94_2:
                    s.b1_SA94_2_2M_isT3Measured = (b & 0x02) > 0;
                    s.b0_SA94_2_isComputeT3Programmed = (b & 0x01) > 0;
                    break;
                case VersionHardware.SA94_2M:
                    s.b1_SA94_2_2M_isT3Measured = (b & 0x02) > 0;
                    s.b0_SA94_1_2M_isPrn1OnBackTube = (b & 0x01) > 0;
                    break;
            }

            return s;
        }

        public NextAddr ParseNextAddrResponse(DeviceResponse answer)
        {
            if (answer.Body.Length != 6) throw new Exception("ответ не распознан (несовпадение по длине)");
            byte[] b = answer.Body;
            NextAddr naddr = new NextAddr();
            naddr.hour = BitConverter.ToUInt16(b, 0);
            naddr.day = BitConverter.ToUInt16(b, 2);
            naddr.abnormal = BitConverter.ToUInt16(b, 4);
            return naddr;
        }

        public TimeSpan ParseTime(byte[] b, int offset)
        {
            if ((offset + 4) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            return new TimeSpan(DriverHelper.BinDecToInt(b[1]), DriverHelper.BinDecToInt(b[2]), DriverHelper.BinDecToInt(b[3]));
        }

        public DateTime ParseDate(byte[] b, int offset)
        {
            if ((offset + 4) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            return new DateTime(2000 + DriverHelper.BinDecToInt(b[3]), DriverHelper.BinDecToInt(b[2]), DriverHelper.BinDecToInt(b[1]));
        }

        public double ParseFloat(byte[] b, int offset)
        {
            if ((offset + 4) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            //if ((answer[0] == 0xFF) && (answer[1] == 0xFF) && (answer[2] == 0xFF) && (answer[3] == 0xFF)) return null;

            byte[] ieeFormat = new byte[4];
            ieeFormat[0] = b[offset + 3];
            ieeFormat[1] = b[offset + 2];
            ieeFormat[2] = (byte)(b[offset + 1] & 0x7F);
            ieeFormat[3] = (byte)(b[offset + 1] & 0x80);

            byte e17 = (byte)(b[offset + 0] >> 1);
            if (e17 > 1)
            {
                ieeFormat[3] |= (byte)(e17 - 1);
            }
            ieeFormat[2] |= (byte)(b[offset + 0] << 7);
            //return result
            return BitConverter.ToSingle(ieeFormat, 0);
        }

        public Version ParseVersion(string ver)
        {
            Version v = new Version();
            v.verText = ver;
            v.isExtended = false;
            v.isM = false;

            if (ver.StartsWith("100")) { v.verSw = VersionSoftware.ver100; v.verHw = VersionHardware.SA94_1; }
            else if (ver.StartsWith("101")) { v.verSw = VersionSoftware.ver101; v.verHw = VersionHardware.SA94_1; v.isExtended = true; }
            else if (ver.StartsWith("200")) { v.verSw = VersionSoftware.ver200; v.verHw = VersionHardware.SA94_2; }
            else if (ver.StartsWith("201")) { v.verSw = VersionSoftware.ver201; v.verHw = VersionHardware.SA94_2; v.isExtended = true; }
            else if (ver.StartsWith("300")) { v.verSw = VersionSoftware.ver300; v.verHw = VersionHardware.SA94_2M; }
            else if (ver.StartsWith("301")) { v.verSw = VersionSoftware.ver301; v.verHw = VersionHardware.SA94_2M; v.isExtended = true; }
            else if (ver.StartsWith("M100")) { v.verSw = VersionSoftware.verM100; v.verHw = VersionHardware.SA94_1; v.isM = true; }
            else if (ver.StartsWith("M101")) { v.verSw = VersionSoftware.verM101; v.verHw = VersionHardware.SA94_1; v.isM = true; v.isExtended = true; }
            else if (ver.StartsWith("MTE1")) { v.verSw = VersionSoftware.verMTE1; v.verHw = VersionHardware.SA94_2; v.isM = true; v.isExtended = true; }
            else if (ver.StartsWith("M300")) { v.verSw = VersionSoftware.verM300; v.verHw = VersionHardware.SA94_2M; v.isM = true; }
            else if (ver.StartsWith("M301")) { v.verSw = VersionSoftware.verM301; v.verHw = VersionHardware.SA94_2M; v.isM = true; v.isExtended = true; }
            else throw new Exception("версия не распознана");
            return v;
        }

        public Version ParseVersion(byte[] b, int offset)
        {
            string ver = Encoding.ASCII.GetString(b.Skip(1).Take(9).ToArray()).Trim();
            return ParseVersion(ver);
        }

        public ParsedParameter[] ParseHourly(byte[] b, int offset, VersionSoftware verSw)
        {
            bool isExtended = false;
            if ((verSw == VersionSoftware.ver100) || (verSw == VersionSoftware.ver200) || (verSw == VersionSoftware.ver300) || (verSw == VersionSoftware.verM100) || (verSw == VersionSoftware.verM300))
            {
                if ((offset + 32) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            }
            else if ((verSw == VersionSoftware.ver101) || (verSw == VersionSoftware.ver201) || (verSw == VersionSoftware.verMTE1) || (verSw == VersionSoftware.ver301) || (verSw == VersionSoftware.verM101) || (verSw == VersionSoftware.verM301))
            {
                if ((offset + 64) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
                isExtended = true;
            }
            else
            {
                throw new Exception("версия не поддерживается");
            }

            List<ParsedParameter> p = new List<ParsedParameter>();
            if ((verSw == VersionSoftware.ver100) || (verSw == VersionSoftware.ver200) || (verSw == VersionSoftware.ver300) //старая статистика
                || (verSw == VersionSoftware.ver101) || (verSw == VersionSoftware.ver201) || (verSw == VersionSoftware.verMTE1) || (verSw == VersionSoftware.ver301))//расширенная статистика
            {
                DateTime date = ParseDate(b, 0);
                date.Add(ParseTime(b, 4));
                p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, 8) });
                p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = ParseFloat(b, 16) });
                p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = ParseFloat(b, 20) });
                p.Add(new ParsedParameter { date = date, parameter = "P", unit = "кВт", value = ParseFloat(b, 28) });
                if ((verSw != VersionSoftware.ver100) && (verSw != VersionSoftware.ver101))
                {
                    p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, 12) });
                    p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = ParseFloat(b, 24) });
                }

                if (isExtended)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, 32) });
                    p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, 36) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1м", unit = "м3", value = ParseFloat(b, 40) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "т", value = ParseFloat(b, 48) });
                    p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, 56) });
                    p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, 60) });
                    if (verSw != VersionSoftware.ver101)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "V2м", unit = "м3", value = ParseFloat(b, 44) });
                        p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "т", value = ParseFloat(b, 52) });
                    }
                }
            }
            else
            {
                DateTime date = ParseDate(b, 0);
                date.AddHours(b[5]);
                p.Add(new ParsedParameter { date = date, parameter = "tрабч", unit = "с", value = BitConverter.ToUInt16(b, 6) });
                p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G1" : "Q1", unit = "т", value = ParseFloat(b, 8) });
                p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = BitConverter.ToInt16(b, 16) * 0.01 });
                p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = BitConverter.ToInt16(b, 18) * 0.01 });
                p.Add(new ParsedParameter { date = date, parameter = "tmax", unit = "с", value = BitConverter.ToUInt16(b, 22) });
                p.Add(new ParsedParameter { date = date, parameter = "tmin", unit = "с", value = BitConverter.ToUInt16(b, 24) });
                p.Add(new ParsedParameter { date = date, parameter = "tdt", unit = "с", value = BitConverter.ToUInt16(b, 26) });
                p.Add(new ParsedParameter { date = date, parameter = "E", unit = "Гкал", value = ParseFloat(b, 28) });
                if (verSw != VersionSoftware.verM100 && verSw != VersionSoftware.verM101)
                {
                    p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G2" : "Q2", unit = "т", value = ParseFloat(b, 12) });
                    p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = BitConverter.ToInt16(b, 20) * 0.01 });
                }

                if (isExtended)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, 32) });
                    p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, 36) });
                    p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, 40) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, 48) });
                    p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, 56) });
                    if (verSw != VersionSoftware.verM101)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, 44) });
                        p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, 52) });
                    }
                }
            }

            return p.ToArray();
        }

        public ParsedParameter[] ParseDailyOld(byte[] b, int offset, VersionSoftware verSw)
        {
            bool isExtended = false;
            if ((verSw == VersionSoftware.ver100) || (verSw == VersionSoftware.ver200) || (verSw == VersionSoftware.ver300) || (verSw == VersionSoftware.verM100) || (verSw == VersionSoftware.verM300))
            {
                if ((offset + 32) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            }
            else if ((verSw == VersionSoftware.ver101) || (verSw == VersionSoftware.ver201) || (verSw == VersionSoftware.verMTE1) || (verSw == VersionSoftware.ver301) || (verSw == VersionSoftware.verM101) || (verSw == VersionSoftware.verM301))
            {
                if ((offset + 64) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
                isExtended = true;
            }
            else
            {
                throw new Exception("версия не поддерживается");
            }

            List<ParsedParameter> p = new List<ParsedParameter>();
            if ((verSw == VersionSoftware.ver100) || (verSw == VersionSoftware.ver200) || (verSw == VersionSoftware.ver300)
                || (verSw == VersionSoftware.ver101) || (verSw == VersionSoftware.ver201) || (verSw == VersionSoftware.verMTE1) || (verSw == VersionSoftware.ver301))
            {
                DateTime date = ParseDate(b, 0);
                if (verSw != VersionSoftware.verMTE1)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, 4) });
                }
                p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, 8) });
                p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = ParseFloat(b, 16) });
                p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = ParseFloat(b, 20) });
                p.Add(new ParsedParameter { date = date, parameter = "P", unit = "кВт", value = ParseFloat(b, 28) });
                if ((verSw != VersionSoftware.ver100) && (verSw != VersionSoftware.ver101))
                {
                    p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, 12) });
                    p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = ParseFloat(b, 24) });
                }

                if (isExtended)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, 32) });
                    p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, 36) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1м", unit = "м3", value = ParseFloat(b, 40) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "т", value = ParseFloat(b, 48) });
                    p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, 56) });
                    //p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, 60) });

                    if (verSw != VersionSoftware.ver101)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "V2м", unit = "м3", value = ParseFloat(b, 44) });
                        p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "т", value = ParseFloat(b, 52) });
                    }
                }
            }
            else
            {
                DateTime date = ParseDate(b, 0);
                p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, 4) });
                p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G1" : "Q1", unit = "т", value = ParseFloat(b, 8) });
                p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = BitConverter.ToInt16(b, 16) * 0.01 });
                p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = BitConverter.ToInt16(b, 18) * 0.01 });
                p.Add(new ParsedParameter { date = date, parameter = "tmax", unit = "с", value = BitConverter.ToUInt16(b, 22) });
                p.Add(new ParsedParameter { date = date, parameter = "tmin", unit = "с", value = BitConverter.ToUInt16(b, 24) });
                p.Add(new ParsedParameter { date = date, parameter = "tdt", unit = "с", value = BitConverter.ToUInt16(b, 26) });
                p.Add(new ParsedParameter { date = date, parameter = "E", unit = "Гкал", value = ParseFloat(b, 28) });
                if ((verSw != VersionSoftware.verM100) && (verSw != VersionSoftware.verM101))
                {
                    p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G2" : "Q2", unit = "т", value = ParseFloat(b, 12) });
                    p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = BitConverter.ToInt16(b, 20) * 0.01 });
                }

                if (isExtended)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, 32) });
                    p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, 36) });
                    p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, 40) });
                    p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, 48) });
                    p.Add(new ParsedParameter { date = date, parameter = "tрабс", unit = "с", value = b[56] + ((UInt32)b[57] << 8) + ((UInt32)b[58] << 16) });

                    if (verSw != VersionSoftware.verM101)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, 44) });
                        p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, 52) });
                    }
                }
            }

            return p.ToArray();
        }


#if OLD_DRIVER
        bool debugMode = false;
#endif

        UInt32 NetworkAddress = 0;
        //Version Ver;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;


        #region Common
        public enum DeviceError
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION
        };

        private void log(string message, int level = 2)
        {
#if OLD_DRIVER
            if ((level < 3) || ((level == 3) && debugMode))
            {
                logger(message);
            }
#else
            logger(message, level);
#endif
        }

        private byte[] SendSimple(byte[] data, int timeoutMaximum = 7500)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var timeout = timeoutMaximum;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
            {
                Thread.Sleep(sleep);

                var buf = response();
                if (buf.Any())
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0;
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == 6)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic SendOld(byte[] data, int timeOut = 7500, int attemptsMaximum = 3)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < attemptsMaximum && answer.success == false; attempts++)
            {
                buffer = SendSimple(data, timeOut);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    //if (buffer.Length < 7)
                    //{
                    //    answer.error = "в кадре ответа не может содежаться менее 7 байт";
                    //    answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    //}
                    //else if (buffer[0] != 0xAA)
                    //{
                    //    answer.error = "Начало кадра не найдено";
                    //    answer.errorcode = DeviceError.ADDRESS_ERROR;
                    //}
                    //else if (buffer[5] != (buffer.Length - 7))
                    //{
                    //    answer.error = "Ожидаемая длина кадра не совпадает с фактической";
                    //    answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                    //}
                    //else if (buffer[3] != data[3] || buffer[4] != data[4])
                    //{
                    //    answer.error = "Получен неизвестный ответ";
                    //    answer.errorcode = DeviceError.CRC_ERROR;
                    //}
                    //else
                    //{
                    answer.success = true;
                    answer.error = string.Empty;
                    answer.errorcode = DeviceError.NO_ERROR;
                    //}
                }
            }

            if (answer.success)
            {
                answer.Body = buffer;//.Skip(5).Take(1 + (buffer.Length - 7)).ToArray();
            }

            return answer;
        }


        private DeviceResponse Send(byte[] data, int timeOut = 7500, int attemptsMaximum = 1)
        {
            DeviceResponse answer = new DeviceResponse();
            byte[] buffer = null;
            for (var attempts = 0; attempts < attemptsMaximum; attempts++)
            {
                buffer = SendSimple(data, timeOut);
                if (buffer.Length == 0) throw new Exception("Нет ответа");
            }

            answer.Body = buffer;
            return answer;
        }


        public static dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public static dynamic MakeResult(int code, DeviceError errorcode = DeviceError.NO_ERROR, string description = "")
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.errorcode = errorcode;
            result.error = description;
            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion

        #region ImportExport
        /// <summary>
        /// Регистр выбора стрраницы
        /// </summary>
        private const short RVS = 0x0084;

#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        private Func<byte[]> response;

        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        [Import("cancel")]
        private Func<bool> cancel;

        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;

        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;

        [Import("setContractHour")]
        private Action<int> setContractHour;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            //setArchiveDepth("Day", 2);

            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion

            //#region version
            //if (!param.ContainsKey("version") || !(arg.version is string))
            //{
            //    log("Отсутствуют сведения о версии устройства", level: 1);
            //    return MakeResult(202, DeviceError.NO_ERROR, "версия устройства");
            //}

            //try
            //{
            //    Ver = ParseVersion(arg.version);
            //}
            //catch(Exception ex)
            //{
            //    log("Не распознана версия устройства, ожидается строка вида \"100\" или \"M301\"", level: 1);
            //    return MakeResult(202, DeviceError.NO_ERROR, "версия устройства");
            //}
            //#endregion

#if OLD_DRIVER
            #region debug
            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
            }
            #endregion
#endif

            #region components
            var components = "Hour;Day;Constant;Abnormal;Current";
            if (param.ContainsKey("components"))
            {
                components = arg.components;
                log(string.Format("указаны архивы {0}", components));
            }
            else
            {
                log(string.Format("архивы не указаны, будут опрошены все"));
            }
            #endregion

            #region start
            if (param.ContainsKey("start") && arg.start is DateTime)
            {
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }
            #endregion

            #region end
            if (param.ContainsKey("end") && arg.end is DateTime)
            {
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }
            #endregion

            #region hourRanges
            List<dynamic> hourRanges;
            if (param.ContainsKey("hourRanges") && arg.hourRanges is IEnumerable<dynamic>)
            {
                hourRanges = arg.hourRanges;
                foreach (var range in hourRanges)
                {
                    log(string.Format("принят часовой диапазон {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", range.start, range.end));
                }
            }
            else
            {
                hourRanges = new List<dynamic>();
                dynamic defaultrange = new ExpandoObject();
                defaultrange.start = getStartDate("Hour");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Hour");
                hourRanges.Add(defaultrange);
            }
            #endregion

            #region dayRanges
            List<dynamic> dayRanges;
            if (param.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
            {
                dayRanges = arg.dayRanges;
                foreach (var range in dayRanges)
                {
                    log(string.Format("принят суточный диапазон {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", range.start, range.end));
                }
            }
            else
            {
                dayRanges = new List<dynamic>();
                dynamic defaultrange = new ExpandoObject();
                defaultrange.start = getStartDate("Day");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Day");
                dayRanges.Add(defaultrange);
            }
            #endregion



            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges));
                        }
                        break;

                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func)
        {
            Send(MakeDisonnectRequest());
            dynamic connect = ParseConnectResponse(Send(MakeConnectRequest()));
            if (!connect.success)
            {
                log("Прибор НЕ обнаружен", level: 1);
                return MakeResult(100, connect.errorcode, connect.error);
            }

            log("Прибор обнаружен");//connect.result;
            dynamic result = func();
            Send(MakeDisonnectRequest());
            return result;
        }

        //private dynamic Wrap(Func<dynamic> func)
        //{
        //    Send(MakeDeviceUnselectRequest());
        //    Status s;
        //    try
        //    {
        //        s = ParseDeviceSelectResponse(Send(MakeDeviceSelectRequest(NetworkAddress)), Ver.verHw);
        //        log("Прибор обнаружен", level: 1);
        //    }
        //    catch(Exception ex)
        //    {
        //        log("Прибор НЕ обнаружен", level: 1);
        //        return MakeResult(100, DeviceError.NO_ERROR, ex.Message);
        //    }

        //    dynamic result = func();
        //    Send(MakeDeviceUnselectRequest());
        //    return result;
        //}
        #endregion


        private byte[] MakeConnectRequest()
        {
            return new byte[] { 0xFF,
                (byte)(((NetworkAddress >> 14) & 0x7F) | 0xC0),
                (byte)((NetworkAddress >> 7) & 0x7F),
                (byte)(NetworkAddress & 0x7F)
            };
        }

        private dynamic ParseConnectResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;

            if (body.Length != 1)
            {
                return MakeResult(100, DeviceError.TOO_SHORT_ANSWER, "получен неизвестный ответ");
            }

            answer.result = body[0];
            return answer;
        }

        private byte[] MakeDisonnectRequest()
        {
            return new byte[] { 0xFF };
        }

        private byte[] MakeRecordRequest(byte[] address)
        {
            return new byte[] { 0xB7, address[0], address[1] };
        }

        private dynamic ParseRecordResponse(dynamic answer, Address address)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;

            if (body.Length != 128)
            {
                return MakeResult(100, DeviceError.TOO_SHORT_ANSWER, "получен неизвестный ответ");
            }

            answer.record = ParserHelper.ParseRecord(body, address);
            return answer;
        }

        private byte[] MakeDeviceFirmwareRequest()
        {
            return new byte[] { 0xB7, 0x40, 0x02 };
        }

        private dynamic ParseDeviceFirmwareResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;

            if (body.Length != 128)
            {
                return MakeResult(100, DeviceError.TOO_SHORT_ANSWER, "получен неизвестный ответ");
            }

            body = body.Take(10).ToArray();
            body[0] = 0x20;
            answer.value = ByteArrayToString(body);
            answer.valueAscii = ConvertHex(answer.value);
            return answer;
        }

        private byte[] MakeCurrentRequest(byte currentAddr)
        {
            return new byte[] { (byte)(0x80 | currentAddr) };
        }

        private dynamic ParseCurrentResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;
            double value = 0.0;
            if (body.Length == 4)
            {
                value = MakeFloat(body);
            }
            answer.value = value;
            return answer;
        }



        public byte[] MakeReadNextRecAdrRequest()
        {
            return new byte[] { 0xB8 };
        }

        public dynamic ParseReadNextRecAdrResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;

            if (body.Length != 6)
            {
                return MakeResult(100, DeviceError.TOO_SHORT_ANSWER, "получен неизвестный ответ");
            }
            return answer;

        }

        private dynamic GetCurrent()
        {
            List<dynamic> recs = new List<dynamic>();
            var commands = new List<Tuple<byte, string, string>>
                {
                    new Tuple<byte, string, string>(0x00, "VolumeWaterConsumption1", "м3/с"),
                    new Tuple<byte, string, string>(0x01, "VolumeWaterConsumption2", "м3/с"),
                    new Tuple<byte, string, string>(0x02, "TemperatureWater1", "град.С"),
                    new Tuple<byte, string, string>(0x03, "TemperatureWater2", "град.С"),
                    new Tuple<byte, string, string>(0x04, "TemperatureWater3", "град.С"),
                    new Tuple<byte, string, string>(0x05, "TemperatureWaterConsumption", "град.С"),
                    new Tuple<byte, string, string>(0x06, "Energy", "кВт"),
                    new Tuple<byte, string, string>(0x07, "HeatWaterConsumption", "МВт*ч"),
                    new Tuple<byte, string, string>(0x08, "VolumeWater1", "м3"),
                    new Tuple<byte, string, string>(0x09, "VolumeWater2", "м3"),
                    //new Tuple<byte, ParameterType, string>(0x0A, ParameterType., string.),//Время
                    //new Tuple<byte, ParameterType, string>(0x0B, ParameterType., string.),//Дата
                    new Tuple<byte, string, string>(0x0C, "TimeWork", "ч"),
                    //new Tuple<byte, ParameterType, string>(0x0D, ParameterType.MassWater3, string.tonn),
                    new Tuple<byte, string, string>(0x0E, "PressureWater1", "МПа"),
                    new Tuple<byte, string, string>(0x0F, "PressureWater2", "МПа"),
                };

            DateTime date = DateTime.Now;
            foreach (var command in commands)
            {
                dynamic current = ParseCurrentResponse(Send(MakeCurrentRequest(command.Item1)));
                if (!current.success)
                {
                    return current;
                }

                recs.Add(MakeCurrentRecord(command.Item2, current.value, command.Item3, date));
            }

            dynamic answer = MakeResult(0);
            answer.records = recs;
            return answer;
        }


        private dynamic DoFindPresentRecordHA(Record firstRecord)
        {
            DateTime date = DateTime.Now;
            Address address = firstRecord.Address.GetNextAddressHA(CalculateStepsHA(date, firstRecord) - 0x01);
            return ParseRecordResponse(Send(MakeRecordRequest(address.MBytes)), address);
        }

        private dynamic DoFindBlockHA(DateTime dateTime, RecordCollection cache)
        {
            dynamic answer = MakeResult(0);
            byte[] result = null;

            while (true)
            {
                Record nearest = cache.GetNearestRecordHA(dateTime);
                int steps = CalculateStepsHA(dateTime, nearest);
                Address newAddress = nearest.Address.GetNextAddressHA(steps);
                dynamic recordResponse = ParseRecordResponse(Send(MakeRecordRequest(newAddress.MBytes)), newAddress);
                if (!recordResponse.success) return recordResponse;

                Record newRecord = recordResponse.record;
                if (newRecord == null || DriverHelper.IsValidRecord(newRecord) == false)
                {
                    break;
                }

                if (DriverHelper.IsValidRecord(newRecord))
                {
                    cache.Add(newRecord);
                }

                if (newRecord.Block1.DateTime == dateTime)
                {
                    result = newRecord.Block1.Bytes;
                    break;
                }
                if (newRecord.Block2.DateTime == dateTime)
                {
                    result = newRecord.Block2.Bytes;
                    break;
                }
                if (cache.IsRecordExist(dateTime))
                {
                    var record = cache.ExistRecord(dateTime);
                    if (dateTime == record.Block1.DateTime)
                    {
                        result = newRecord.Block1.Bytes;
                        break;
                    }
                    if (dateTime == record.Block2.DateTime)
                    {
                        result = newRecord.Block2.Bytes;
                        break;
                    }
                    break;
                }
            }

            answer.result = result;
            return answer;
        }

        private dynamic DoFindBlockDA(DateTime date, RecordCollection cache)
        {
            dynamic answer = MakeResult(0);
            byte[] result = null;

            while (true)
            {
                Record nearest = cache.GetNearestRecordDA(date);
                int steps = CalculateStepsDA(date, nearest);
                Address newAddress = nearest.Address.GetNextAddressDA(steps);
                dynamic recordResponse = ParseRecordResponse(Send(MakeRecordRequest(newAddress.MBytes)), newAddress);
                if (!recordResponse.success) return recordResponse;

                Record newRecord = recordResponse.record;
                if (newRecord == null || DriverHelper.IsValidRecord(newRecord) == false)
                {
                    break;
                }

                if (DriverHelper.IsValidRecord(newRecord))
                {
                    cache.Add(newRecord);
                }

                if (newRecord.Block1.Date == date)
                {
                    result = newRecord.Block1.Bytes;
                    break;
                }
                if (newRecord.Block2.Date == date)
                {
                    result = newRecord.Block2.Bytes;
                    break;
                }
                if (cache.IsRecordExist(date))
                {
                    var record = cache.ExistRecord(date);
                    if (date == record.Block1.Date)
                    {
                        result = newRecord.Block1.Bytes;
                        break;
                    }
                    if (date == record.Block2.Date)
                    {
                        result = newRecord.Block2.Bytes;
                        break;
                    }
                    break;
                }

            }

            answer.result = result;
            return answer;
        }

        private dynamic DoFindPresentRecordDA(Record firstRecord)
        {
            if (firstRecord == null)
            {
                dynamic ret = new ExpandoObject();
                ret.success = false;
                ret.errorcode = DeviceError.NO_ERROR;
                ret.error = "Не удалось найти суточные данные - не найдена первая запись";
                return ret;
            }
            DateTime date = DateTime.Today;
            Address address = firstRecord.Address.GetNextAddressDA(CalculateStepsDA(date, firstRecord) - 0x01);

            log($"Получен адрес на сегодня - {((address == null) || (address.MBytes == null) ? "NULL" : string.Join(" ", address.MBytes.Select(b => $"{b:X2}")))}");
            return ParseRecordResponse(Send(MakeRecordRequest(address.MBytes)), address);
        }

        private dynamic GetHourly(DateTime start, DateTime end)
        {
            List<dynamic> allRecords = new List<dynamic>();

            var cache = new RecordCollection();
            Address address = Address.GetFirstHourAddress();
            dynamic recordResponse = ParseRecordResponse(Send(MakeRecordRequest(address.MBytes)), address);
            if (!recordResponse.success) return recordResponse;

            var firstRecord = recordResponse.record;
            if (firstRecord != null) cache.Add(firstRecord);

            recordResponse = DoFindPresentRecordHA(firstRecord);
            if (!recordResponse.success) return recordResponse;

            var presentRecord = recordResponse.record;
            if (presentRecord != null) cache.Add(presentRecord);

            for (DateTime date = start.Date.AddHours(start.Hour); date <= end; date = date.AddHours(1))
            {
                if (cancel())
                {
                    return MakeResult(200, DeviceError.NO_ERROR, "опрос отменён");
                }

                recordResponse = DoFindBlockHA(date, cache);
                if (!recordResponse.success) return recordResponse;

                var recs = ParseHA(recordResponse.result, date);
                if (recs != null)
                {
                    log($"прочитана часовая запись за {date:dd.MM.yyyy HH:mm}");
                    allRecords.AddRange(recs);
                    records(recs);
                }
                else
                {
                    log($"часовая запись за {date:dd.MM.yyyy HH:mm} НЕ прочитана");
                }
            }

            dynamic answer = MakeResult(0);
            answer.records = allRecords;
            return answer;
        }


        private dynamic GetDaily(DateTime start, DateTime end)
        {
            List<dynamic> allRecords = new List<dynamic>();
            var cache = new RecordCollection();

            Address address = Address.GetFirstDayAddress();
            log($"Получен адрес первого дня - {((address == null) || (address.MBytes == null) ? "NULL" : string.Join(" ", address.MBytes.Select(b => $"{b:X2}")))}");

            dynamic recordResponse = ParseRecordResponse(Send(MakeRecordRequest(address.MBytes)), address);
            if (!recordResponse.success) return recordResponse;

            Record firstRecord = recordResponse.record as Record;
            if (firstRecord != null) cache.Add(firstRecord);

            log($"Чтение первой записи - {(firstRecord == null ? "NULL" : $"Block1={{date={firstRecord.Block1.Date:dd.MM.yyyy HH:mm},isvalid={firstRecord.Block1.IsValid}}} Block2={{date={firstRecord.Block2.Date:dd.MM.yyyy HH:mm},isvalid={firstRecord.Block2.IsValid}}}")}");

            recordResponse = DoFindPresentRecordDA(firstRecord);
            if (!recordResponse.success) return recordResponse;

            var presentRecord = recordResponse.record;
            if (presentRecord != null) cache.Add(presentRecord);

            for (DateTime date = start.Date; date <= end; date = date.AddDays(1))
            {
                if (cancel())
                {
                    return MakeResult(200, DeviceError.NO_ERROR, "опрос отменён");
                }

                recordResponse = DoFindBlockDA(date, cache);
                if (!recordResponse.success) return recordResponse;

                var recs = ParseDA(recordResponse.result, date);
                if (recs != null)
                {
                    log($"прочитана суточная запись за {date:dd.MM.yyyy}");
                    allRecords.AddRange(recs);
                    records(recs);
                }
                else
                {
                    log($"суточная запись за {date:dd.MM.yyyy} НЕ прочитана");
                }
            }

            dynamic answer = MakeResult(0);
            answer.records = allRecords;
            return answer;
        }




        public static byte ByteLow(int getLow)
        {
            return (byte)(getLow & 0xFF);
        }
        public static byte ByteHigh(int getHigh)
        {
            return (byte)((getHigh >> 8) & 0xFF);
        }


        public static int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }

        private float MakeFloat(byte[] answer, int startIndex)
        {
            if (answer == null || answer.Length < startIndex + 4) return 0;
            return MakeFloat(new byte[] { answer[startIndex], answer[startIndex + 1], answer[startIndex + 2], answer[startIndex + 3] });
        }

        private float MakeFloat(byte[] answer)
        {
            if (answer == null || answer.Length != 4) return 0;
            byte[] ieeFormat = new byte[4];

            //result[0] = pv[3];
            //result[1] = pv[2];
            //result[2] = pv[1] & 0x7F;
            //result[3] = pv[1] & 0x80;

            ieeFormat[0] = answer[3];
            ieeFormat[1] = answer[2];
            ieeFormat[2] = (byte)(answer[1] & 0x7F);
            ieeFormat[3] = (byte)(answer[1] & 0x80);

            byte e17 = (byte)(answer[0] >> 1);
            if (e17 > 1)
            {
                ieeFormat[3] |= (byte)(e17 - 1);
            }
            ieeFormat[2] |= (byte)(answer[0] << 7);
            //return result
            return BitConverter.ToSingle(ieeFormat, 0);
        }


        //public override SurveyResult Ping()
        //{
        //    if (Connect())
        //    {
        //        DeSelectDevice(2000);
        //        ReadDeviceFirmware();
        //        return new SurveyResult { State = SurveyResultState.Success };
        //    }
        //    return new SurveyResult { State = SurveyResultState.NoResponse };
        //}


        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public string ConvertHex(String hexString)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string hs = hexString.Substring(i, 2);
                sb.Append(Convert.ToChar(Convert.ToUInt32(hs, 16)));
            }

            String ascii = sb.ToString();
            return ascii;
        }


        private int CalculateStepsDA(DateTime dateTime, Record record)
        {
            var i = ((dateTime - record.Block1.Date).TotalDays);
            if ((i != 1) && (i != -1))
            {
                i = i / 2;
            }

            return ((int)(i));

        }

        private IEnumerable<dynamic> ParseHA(byte[] bytes, DateTime dateTime)
        {
            if (bytes == null) return null;
            return DriverHelper.ParseHourArchive(bytes, 0, dateTime);
        }

        private IEnumerable<dynamic> ParseDA(byte[] bytes, DateTime dateTime)
        {
            if (bytes == null) return null;
            return DriverHelper.ParseDayArchive(bytes, 0, dateTime);
        }


        private int CalculateStepsHA(DateTime dateTime, Record record)
        {
            var i = ((dateTime - record.Block1.DateTime).TotalHours);

            if ((i != 1) && (i != -1))
            {
                i = i / 2;
            }

            return ((int)(i));
        }




        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            var currentDate = DateTime.Now;
            setTimeDifference(DateTime.Now - currentDate);

            //log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", currentDate));

            if (getEndDate == null)
            {
                getEndDate = (type) => currentDate;
            }

            if (components.Contains("Hour"))
            {
                List<dynamic> hours = new List<dynamic>();
                if (hourRanges != null)
                {
                    foreach (var range in hourRanges)
                    {
                        var startH = range.start;
                        var endH = range.end;

                        if (startH > currentDate) continue;
                        if (endH > currentDate) endH = currentDate;

                        var hour = GetHourly(startH, endH);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                            return MakeResult(105, hour.errorcode, hour.error);
                        }
                        hours.AddRange(hour.records);

                        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                    }
                }
                else
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");

                    var hour = GetHourly(startH, endH);
                    if (!hour.success)
                    {
                        log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        return MakeResult(105, hour.errorcode, hour.error);
                    }
                    hours.AddRange(hour.records);

                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                }
            }

            if (components.Contains("Day"))
            {
                List<dynamic> days = new List<dynamic>();
                if (dayRanges != null)
                {
                    foreach (var range in dayRanges)
                    {
                        var startD = range.start;
                        var endD = range.end;

                        if (startD > currentDate) continue;
                        if (endD > currentDate) endD = currentDate;

                        var day = GetDaily(startD.Date, endD);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.errorcode, day.error);
                        }
                        days.AddRange(day.records);

                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                else
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    var day = GetDaily(startD.Date, endD);
                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        return MakeResult(104, day.errorcode, day.error);
                    }
                    days.AddRange(day.records);

                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }
            }



            //    /// Нештатные ситуации ///
            //    if (components.Contains("Abnormal"))
            //    {
            //        var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
            //        var startAbnormal = lastAbnormal.Date;

            //        var endAbnormal = getEndDate("Abnormal");
            //        byte[] codes = new byte[] { };

            //        List<dynamic> abnormals = new List<dynamic>();


            //        log(string.Format("получено {0} записей НС за период", abnormals.Count));//{1:dd.MM.yy}, date));
            //        records(abnormals);
            //    }
            //}
            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }
    }
}
