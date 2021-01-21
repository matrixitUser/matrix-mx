using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public static class Parser
    {
        public static int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }

        public static byte ByteToBCD(byte dec)
        {
            byte b0 = (byte)(dec % 10);
            byte b1 = (byte)((dec / 10) % 10);
            return (byte)((b1 << 4) | b0);
        }

        public static Status ParseDeviceSelectResponse(DeviceResponse answer, VersionHardware verHw)
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
                    s.b1_SA94_1_isT2Programmed = (b & 0x02) > 0;
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

        public static NextAddr ParseNextAddrResponse(DeviceResponse answer)
        {
            if (answer.Body.Length != 6) throw new Exception("ответ не распознан (несовпадение по длине)");
            byte[] b = answer.Body;
            NextAddr naddr = new NextAddr();
            naddr.hour = BitConverter.ToUInt16(b, 0);
            naddr.day = BitConverter.ToUInt16(b, 2);
            naddr.abnormal = BitConverter.ToUInt16(b, 4);
            return naddr;
        }

        public static TimeSpan ParseTime(byte[] b, int offset)//, Action<string, int> log = null
        {
            if ((offset + 4) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            //if(log != null) log($"парсинг времени {BinDecToInt(b[offset + 1])}:{BinDecToInt(b[offset + 2])}:{BinDecToInt(b[offset + 3])} => {new TimeSpan(BinDecToInt(b[offset + 1]), BinDecToInt(b[offset + 2]), BinDecToInt(b[offset + 3]))}", 2);
            return new TimeSpan(BinDecToInt(b[offset + 1]), BinDecToInt(b[offset + 2]), BinDecToInt(b[offset + 3]));
        }

        public static DateTime ParseDate(byte[] b, int offset, Action<string, int> log = null)//
        {
            if ((offset + 4) > b.Length) throw new Exception("ответ не распознан (несовпадение по длине)");
            if (b[offset + 3] == 0xFF || b[offset + 2] == 0xFF || b[offset + 1] == 0xFF)
            {
                return DateTime.MaxValue;
            }
            //if (log != null) log($"парсинг даты {BinDecToInt(b[offset + 1])}.{BinDecToInt(b[offset + 2])}.{BinDecToInt(b[offset + 3])} => {new DateTime(2000 + BinDecToInt(b[offset + 3]), BinDecToInt(b[offset + 2]), BinDecToInt(b[offset + 1]))}", 2);
            return new DateTime(2000 + BinDecToInt(b[offset + 3]), BinDecToInt(b[offset + 2]), BinDecToInt(b[offset + 1]));
        }

        public static double ParseFloat(byte[] b, int offset)
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

        public static Version ParseVersion(string ver, Action<string, int> log = null)
        {
            Version v = new Version();
            v.verText = ver;
            v.isExtended = false;
            v.isM = false;
            //if(log != null) log($"версия \"{ver}\" индекс {ver.IndexOf("-")} субверсия {(ver.Contains("-") ? int.Parse(ver.Substring(ver.IndexOf("-") + 1).Trim()) : 0)}", 1);
            try
            {
                v.subVer = ver.Contains("-") ? int.Parse(ver.Substring(ver.IndexOf("-") + 1)) : 0;
            }
            catch (Exception ex)
            {

            }

            if (ver.StartsWith("100")) { v.verSw = VersionSoftware.ver100; v.verHw = VersionHardware.SA94_1; }
            else if (ver.StartsWith("101")) { v.verSw = VersionSoftware.ver101; v.verHw = VersionHardware.SA94_1; v.isExtended = true; }
            else if (ver.StartsWith("200")) { v.verSw = VersionSoftware.ver200; v.verHw = VersionHardware.SA94_2; }
            else if (ver.StartsWith("201")) { v.verSw = VersionSoftware.ver201; v.verHw = VersionHardware.SA94_2; v.isExtended = true; }
            else if (ver.StartsWith("300")) { v.verSw = VersionSoftware.ver300; v.verHw = VersionHardware.SA94_2M; }
            else if (ver.StartsWith("301")) { v.verSw = VersionSoftware.ver301; v.verHw = VersionHardware.SA94_2M; v.isExtended = true; }
            else if (ver.StartsWith("M100")) { v.verSw = VersionSoftware.verM100; v.verHw = VersionHardware.SA94_1; v.isM = true; }
            else if (ver.StartsWith("M101")) { v.verSw = VersionSoftware.verM101; v.verHw = VersionHardware.SA94_1; v.isM = true; v.isExtended = true; if (v.subVer >= 4) v.hasReadNextAddrStatCmd = true; }
            else if (ver.StartsWith("MTE1")) { v.verSw = VersionSoftware.verMTE1; v.verHw = VersionHardware.SA94_2; v.isM = true; v.isExtended = true; if (v.subVer >= 1) v.hasReadNextAddrStatCmd = true; }
            else if (ver.StartsWith("M300")) { v.verSw = VersionSoftware.verM300; v.verHw = VersionHardware.SA94_2M; v.isM = true; }
            else if (ver.StartsWith("M301")) { v.verSw = VersionSoftware.verM301; v.verHw = VersionHardware.SA94_2M; v.isM = true; v.isExtended = true; if (v.subVer >= 4) v.hasReadNextAddrStatCmd = true; }
            else throw new Exception("версия не распознана");
            return v;
        }

        public static Version ParseVersion(byte[] b, int offset, Action<string, int> log = null)
        {
            string ver = Encoding.ASCII.GetString(b.Skip(1).Take(9).ToArray()).Trim();
            return ParseVersion(ver, log);
        }




        public static ParsedParameter[] ParseHourlyBlock(byte[] bytes, int offset, Version ver)
        {
            List<ParsedParameter> ppars = new List<SA94.ParsedParameter>();
            for (int i = 0; i < Version.BlockSize; i += ver.GetRecordSize())
            {
                ppars.AddRange(ParseHourly(bytes, offset + i, ver.verSw));
            }
            return ppars.ToArray();
        }

        public static ParsedParameter[] ParseDailyBlock(byte[] bytes, int offset, Version ver, Action<string, int> log = null)
        {
            List<ParsedParameter> ppars = new List<SA94.ParsedParameter>();
            for (int i = 0; i < Version.BlockSize; i += ver.GetRecordSize())
            {
                //if(log != null) log($"parseDaily: offset{offset + i} bytes {string.Join(" ", bytes.Skip(offset + i).Take(5).Select(b => $"{b:X2}"))}...", 2);
                ppars.AddRange(ParseDaily(bytes, offset + i, ver.verSw, log));
            }
            return ppars.ToArray();
        }

        public static ParsedParameter[] ParseHourly(byte[] b, int offset, VersionSoftware verSw)
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
                DateTime date = ParseDate(b, offset + 0);
                if (date != DateTime.MaxValue)
                {
                    date.Add(ParseTime(b, offset + 4));
                    p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, offset + 8) });
                    p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = ParseFloat(b, offset + 16) });
                    p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = ParseFloat(b, offset + 20) });
                    p.Add(new ParsedParameter { date = date, parameter = "P", unit = "кВт", value = ParseFloat(b, offset + 28) });
                    if ((verSw != VersionSoftware.ver100) && (verSw != VersionSoftware.ver101))
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, offset + 12) });
                        p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = ParseFloat(b, offset + 24) });
                    }

                    if (isExtended)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, offset + 32) });
                        p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, offset + 36) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, offset + 40) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1т", unit = "т", value = ParseFloat(b, offset + 48) });
                        p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, offset + 56) });
                        p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, offset + 60) });
                        if (verSw != VersionSoftware.ver101)
                        {
                            p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, offset + 44) });
                            p.Add(new ParsedParameter { date = date, parameter = "V2т", unit = "т", value = ParseFloat(b, offset + 52) });
                        }
                    }
                }
            }
            else
            {
                DateTime date = ParseDate(b, offset + 0);
                if (date != DateTime.MaxValue)
                {
                    date = date.AddHours(BinDecToInt(b[offset + 5]));
                    p.Add(new ParsedParameter { date = date, parameter = "tрабч", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 6) });
                    p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G1" : "Q1", unit = "т", value = ParseFloat(b, offset + 8) });
                    p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 16) * 0.01 });
                    p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 18) * 0.01 });
                    p.Add(new ParsedParameter { date = date, parameter = "tmax", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 22) });
                    p.Add(new ParsedParameter { date = date, parameter = "tmin", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 24) });
                    p.Add(new ParsedParameter { date = date, parameter = "tdt", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 26) });
                    p.Add(new ParsedParameter { date = date, parameter = "E", unit = "Гкал", value = ParseFloat(b, offset + 28) });
                    if (verSw != VersionSoftware.verM100 && verSw != VersionSoftware.verM101)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G2" : "Q2", unit = "т", value = ParseFloat(b, offset + 12) });
                        p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 20) * 0.01 });
                    }

                    if (isExtended)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, offset + 32) });
                        p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, offset + 36) });
                        p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, offset + 40) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, offset + 48) });
                        p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, offset + 56) });
                        if (verSw != VersionSoftware.verM101)
                        {
                            p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, offset + 44) });
                            p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, offset + 52) });
                        }
                    }
                }
            }

            return p.ToArray();
        }

        public static ParsedParameter[] ParseDaily(byte[] b, int offset, VersionSoftware verSw, Action<string, int> log = null)
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
                //if (log != null) log($"дата: байты {string.Join(" ", b.Skip(offset).Take(5).Select(a => $"{a: X2}"))}... после парсинга {ParseDate(b, offset + 0)}", 2);
                DateTime date = ParseDate(b, offset + 0, log);
                if (date != DateTime.MaxValue)
                {
                    if (verSw != VersionSoftware.verMTE1)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, offset + 4) });
                    }
                    p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, offset + 8) });
                    p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = ParseFloat(b, offset + 16) });
                    p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = ParseFloat(b, offset + 20) });
                    p.Add(new ParsedParameter { date = date, parameter = "P", unit = "кВт", value = ParseFloat(b, offset + 28) });
                    if ((verSw != VersionSoftware.ver100) && (verSw != VersionSoftware.ver101))
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, offset + 12) });
                        p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = ParseFloat(b, offset + 24) });
                    }

                    if (isExtended)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, offset + 32) });
                        p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, offset + 36) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, offset + 40) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1т", unit = "т", value = ParseFloat(b, offset + 48) });
                        p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, offset + 56) });
                        //p.Add(new ParsedParameter { date = date, parameter = "E", unit = "МВт*ч", value = ParseFloat(b, 60) });

                        if (verSw != VersionSoftware.ver101)
                        {
                            p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, offset + 44) });
                            p.Add(new ParsedParameter { date = date, parameter = "V2т", unit = "т", value = ParseFloat(b, offset + 52) });
                        }
                    }
                }
            }
            else
            {
                //if (log != null) log($"дата: байты {string.Join(" ", b.Skip(offset).Take(5).Select(a => $"{a: X2}"))}... после парсинга {ParseDate(b, offset + 0)}", 2);
                DateTime date = ParseDate(b, offset + 0, log);
                if (date != DateTime.MaxValue)
                {
                    p.Add(new ParsedParameter { date = date, parameter = "tраб", unit = "ч", value = ParseFloat(b, offset + 4) });
                    p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G1" : "Q1", unit = "т", value = ParseFloat(b, offset + 8) });
                    p.Add(new ParsedParameter { date = date, parameter = "T1", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 16) * 0.01 });
                    p.Add(new ParsedParameter { date = date, parameter = "T2", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 18) * 0.01 });
                    p.Add(new ParsedParameter { date = date, parameter = "tmax", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 22) });
                    p.Add(new ParsedParameter { date = date, parameter = "tmin", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 24) });
                    p.Add(new ParsedParameter { date = date, parameter = "tdt", unit = "с", value = Helper.ToUInt16Reverse(b, offset + 26) });
                    p.Add(new ParsedParameter { date = date, parameter = "E", unit = "Гкал", value = ParseFloat(b, offset + 28) });
                    if ((verSw != VersionSoftware.verM100) && (verSw != VersionSoftware.verM101))
                    {
                        p.Add(new ParsedParameter { date = date, parameter = isExtended ? "G2" : "Q2", unit = "т", value = ParseFloat(b, offset + 12) });
                        p.Add(new ParsedParameter { date = date, parameter = "T3", unit = "°C", value = Helper.ToInt16Reverse(b, offset + 20) * 0.01 });
                    }

                    if (isExtended)
                    {
                        p.Add(new ParsedParameter { date = date, parameter = "p1", unit = "МПа", value = ParseFloat(b, offset + 32) });
                        p.Add(new ParsedParameter { date = date, parameter = "p2", unit = "МПа", value = ParseFloat(b, offset + 36) });
                        p.Add(new ParsedParameter { date = date, parameter = "Q1", unit = "м3/ч", value = ParseFloat(b, offset + 40) });
                        p.Add(new ParsedParameter { date = date, parameter = "V1", unit = "м3", value = ParseFloat(b, offset + 48) });
                        p.Add(new ParsedParameter { date = date, parameter = "tрабс", unit = "с", value = b[offset + 56] + ((UInt32)b[offset + 57] << 8) + ((UInt32)b[offset + 58] << 16) });

                        if (verSw != VersionSoftware.verM101)
                        {
                            p.Add(new ParsedParameter { date = date, parameter = "Q2", unit = "м3/ч", value = ParseFloat(b, offset + 44) });
                            p.Add(new ParsedParameter { date = date, parameter = "V2", unit = "м3", value = ParseFloat(b, offset + 52) });
                        }
                    }
                }
            }

            return p.ToArray();
        }

    }
}
