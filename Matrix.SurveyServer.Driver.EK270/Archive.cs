using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    partial class Driver
    {
        enum RecordType
        {
            Hour, Day, Unknown
        }

        private dynamic GetArchiveRecord(DateTime date, DevType dev, float ver, bool isDay = false, bool tsDay = false)
        {
            dynamic archive;

            if(testMode)
            {
                byte[] data = SendBraces(MakeArchiveRequest(isDay ? 7 : 3, date.Date, date.Date.AddDays(1).AddSeconds(-1), 10));

                var rsp = ParseArchiveResponse(data);
                if (!rsp.success)
                {
                    return rsp;
                }

                archive = ParseArchiveRecordsGrouped(rsp.rows, dev, ver, tsDay);                
            }
            else
            {
                byte[] data = SendBraces(MakeArchiveRequest(isDay ? 7 : 3, date.AddSeconds(-5), date.AddSeconds(5), 1));
                var rsp = ParseArchiveResponse(data);
                if (!rsp.success)
                {
                    return rsp;
                }

                archive = ParseArchiveRecords(rsp.rows, dev, ver, tsDay);
            }
            
            return archive;
        }

        private Dictionary<DateTime, List<dynamic>> cache = new Dictionary<DateTime, List<dynamic>>();

        /// <summary>
        /// Получить запись на определённую дату с поиском в кэше и в архиве
        /// </summary>
        /// <param name="date"></param>
        /// <param name="dev"></param>
        /// <param name="ver"></param>
        /// <param name="isDay"></param>
        /// <param name="tsDay"></param>
        /// <returns></returns>
        private dynamic GetArchiveRecordCache(DateTime dateCh, DevType dev, float ver, bool isDay = false, bool tsDay = false)
        {
            dynamic ret = new ExpandoObject();
            ret.error = string.Empty;
            ret.success = false;

            if (!isDay)
            {
                var data = SendBraces(MakeArchiveRequest(isDay ? 7 : 3, dateCh, dateCh, 1));
                var rsp = ParseArchiveResponse(data);
                if (!rsp.success) return rsp;

                var archive = ParseArchiveRecords(rsp.rows, dev, ver, tsDay);
                return archive;
            }
            else
            {
                //поиск в кэше
                if(cache.ContainsKey(dateCh))
                {
                    log(string.Format("{0:dd.MM.yy}/{0:HH} найден в кэше ", dateCh), level: 3);
                    ret.success = true;
                    ret.records = cache[dateCh];
                    return ret;
                }

                //запрос на получение записи из прибора
                byte[] data;
                
                if(testMode)
                {
                    data = SendBraces(MakeArchiveRequest(isDay ? 7 : 3, dateCh.Date, dateCh.Date.AddDays(1).AddSeconds(-1), 10));
                }
                else
                {
                    data = SendBraces(MakeArchiveRequest(isDay ? 7 : 3, dateCh, dateCh, 10));
                }

                var rsp = ParseArchiveResponse(data);

                for(var i=0; i<200; i++)
                {
                    if (cancel())
                    {
                        ret.error = "отменено";
                        return ret;
                    }

                    log(string.Format("{0:dd.MM.yy}/{0:HH} запрос{1} из устройства: {2}", dateCh, i > 0? (" " + i.ToString()) : "", rsp.success? "получение данных" : rsp.error));
                    if (!rsp.success) return rsp;

                    //Получение записей
                    var archive = ParseArchiveRecordsGrouped(rsp.rows, dev, ver, tsDay);
                    if (!archive.success) return archive;

                    var records = (archive.records as Dictionary<DateTime, List<dynamic>>);
                    
                    //

                    var texts = new List<string>();
                    foreach (var rec in records)
                    {
                        cache[rec.Key] = rec.Value;
                        texts.Add(string.Format("{0:dd.MM.yy}/{0:HH}", rec.Key));
                    }
                    log(string.Format("получены записи за {0}", string.Join(";", texts)));
                    
                    if (cache.ContainsKey(dateCh))
                    {
                        log(string.Format("{0:dd.MM.yy} найден в кэше ", dateCh), level: 3);
                        ret.success = true;
                        ret.records = cache[dateCh];
                        return ret;
                    }

                    rsp = ParseArchiveResponse(SendBraces(new byte[] { ACK }));
                }

                ret.error = "запись не найдена";
                return ret;//должен содержать records
            }

            //return null;
        }


        private dynamic ParseArchiveRecordsGrouped(List<string> groups, DevType devType, float version, bool isDay = false)
        {
            dynamic archive = new ExpandoObject();
            archive.badChannel = false;
            archive.success = true;
            archive.error = string.Empty;
            archive.isEmpty = false;
            archive.isLock = false;

            var recordsByDate = new Dictionary<DateTime, List<dynamic>>();

            // groups = { "(...)(CRC Ok) (...)(CRC Ok) (...)(CRC Ok)", "(...)(CRC Ok) (...)(CRC Ok) (...)(CRC Ok)", "(...)(CRC Ok) (...)(CRC Ok) (...)(CRC Ok)" }
            foreach (var group in groups) 
            {
                //group = "(...)(CRC Ok) (...)(CRC Ok) (...)(CRC Ok)"
                //rows = { "(...)(CRC Ok)", "(...)(CRC Ok)", "(...)(CRC Ok)" }
                var rows = group.Replace("(CRC Ok) (", "(CRC Ok)\n(").Split('\n');

                foreach (var row in rows)
                {
                    //row = "(1)(2)(3)(CRC Ok)"
                    //cells = { "1", "2", "3" }
                    var cells = row.Replace(")(", "\n").Replace("(", "").Replace(")", "\n").Split('\n');

                    dynamic rowRecords = null;

                    var crcOk = cells.FirstOrDefault(c => c.Contains("CRC Ok"));
                    if (crcOk == null)
                    {
                        log(string.Format("парсинг {0}: не содержит контрольную сумму", row), level: 3);
                        continue;
                    }

                    switch (devType)
                    {
                        case DevType.EK260:
                            {
                                rowRecords = ParseCellsArchiveEK260(cells);
                            }
                            break;
                        case DevType.EK270:
                            {
                                rowRecords = ParseCellsArchiveEK270(cells);
                            }
                            break;
                        case DevType.TC210:
                        case DevType.TC215:
                        case DevType.TC220:
                            {
                                rowRecords = ParseCellsArchiveTC2xx(cells, isDay);
                            }
                            break;
                        default:
                            dynamic ar = new ExpandoObject();
                            ar.success = false;
                            ar.badChannel = false;
                            ar.error = string.Format("неподдерживаемый тип прибора {0} вер. {1}", devType, version);
                            return ar;
                    }

                    if (rowRecords == null) continue;
                    if (rowRecords.success)
                    {
                        recordsByDate[rowRecords.dateRaw] = rowRecords.records;
                        log(string.Format("получена {1} запись за {0:dd.MM.yyyy HH:mm}", rowRecords.dateRaw, rowRecords.type == RecordType.Day? "суточная" : (rowRecords.type == RecordType.Hour ? "часовая" : "неизвестная")));
                    }
                    else if(rowRecords.badChannel)
                    {
                        return rowRecords;
                    }
                    else
                    {
                        log(rowRecords.error);
                    }
                }
            }

            if (!recordsByDate.Any())
            {
                archive.success = false;
                archive.error = "записи не найдены";
                archive.badChannel = false;
            }

            archive.records = recordsByDate;
            return archive;
        }


        private byte[] MakeArchiveRequest(int archiveNumber, DateTime start, DateTime end, int count)
        {
            var encoding = Encoding.ASCII;

            var bytes = new List<byte>();
            bytes.Add(SOH);
            bytes.AddRange(encoding.GetBytes("R3"));
            bytes.Add(STX);
            var parameters = string.Format("{0}:V.{1}({2};{3:yyyy-MM-dd,HH:mm:ss};{4:yyyy-MM-dd,HH:mm:ss};{5})", archiveNumber, 0, 3, start, end, count);
            //  log(string.Format("чтение архива {0}", parameters));
            bytes.AddRange(encoding.GetBytes(parameters));
            bytes.Add(ETX);
            //  bytes.AddRange(Crc.Calc(bytes.ToArray(), 1, bytes.Count - 1, new BccCalculator()).CrcData);
            bytes.Add(CalcCrc(bytes.ToArray(), 1, bytes.Count - 1));
            return bytes.ToArray();
        }

        private dynamic ParseValueArchiveResponse2(byte[] bytes, float version, DevType devType)
        {
            dynamic archive = new ExpandoObject();
            //archive.records = new List<dynamic>();

            if (!bytes.Any())
            {
                archive.success = false;
                archive.error = "не получен ответ на команду";
                return archive;
            }

            if (bytes.Count() < 3)
            {
                archive.success = false;
                archive.error = "слишком короткий ответ на команду";
                return archive;
            }

            archive.success = true;
            archive.error = string.Empty;

            //исключаем начальный STX
            var str = Encoding.ASCII.GetString(bytes, 1, bytes.Length - 1);

            archive.IsUncnownRecordType = false;

            //log(string.Format("весь {0}", str));

            char stx = (char)STX;
            var rows = str.Split(stx);
            var records = new List<dynamic>();

            foreach (var row in rows)
            {
                try
                {
                    var cells = row.Replace(")(", "\n").Replace("(", "").Replace(")", "").Split('\n');

                    if (devType == DevType.EK260)
                    {
                        if (version > 2.0)
                        {
                            log(string.Format("ЕК260 v.2.XX парсинг {0}", row));

                            var evt = cells[cells.Length - 2];

                            var type = RecordType.Hour;
                            var typeStr = "Hour";
                            var vnr = 0;

                            log(string.Format("evt = {0}", evt));
                            switch (evt)
                            {
                                case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                                case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
                            }

                            if (type == RecordType.Unknown)
                            {
                                log(string.Format("мимо"));
                                continue;
                            }

                            var date = ParseDate(cells[2]);

                            switch (type)
                            {
                                case RecordType.Day:
                                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                    break;
                                case RecordType.Hour:
                                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                    break;
                            }
                            int pos = -1;
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "", date));
                            var strdate = cells[++pos];//дата
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[++pos]), "м³", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[++pos]), "м³", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[++pos]), "м³", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[++pos]), "м³", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[++pos]), "Bar", date));                            

                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[++pos]), "°C", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.CMP, ParseFloat(cells[++pos]), "", date));
                            if (cells.Length == 20)
                            {
                                records.Add(MakeDayOrHourRecord(typeStr, Glossary.dpTe, ParseFloat(cells[++pos]), "", date));
                                records.Add(MakeDayOrHourRecord(typeStr, Glossary.T2Tek, ParseFloat(cells[++pos]), "", date));
                            }
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St2, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St4, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St7, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St6, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.StSy, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));
                            //log(string.Format("{0} прочитано {1} обработано {2} записей: ср.давл={3}, ср.темп={4} тип записи кстати {5}",
                            //    strdate, cells.Length, records.Count(), records[6].d1, records[7].d1, typeStr));
                        }
                        else if (version < 1.11)
                        {
                            log(string.Format("версия {0}", version));
                        }
                        else if (version > 20)
                        {
                            log(string.Format("версия {0}", version));
                        }
                    }
                    else if (devType == DevType.EK270)
                    {
                        log(string.Format("ЕК270 парсинг {0}", row));
                        //(18484)(640)(2014-05-02,10:00:00)(882066.6309)(884266.5191)(67859)(68044)(12.9437)(10.16)(0.97417)(13.57199)(7)(14)(0)(0)(0)(0x8103)(CRC Ok)
                        var evt = cells[cells.Length - 2];

                        var type = RecordType.Unknown;
                        var typeStr = "";
                        var vnr = 0;
                        log(string.Format("evt = {0}", evt));
                        switch (evt)
                        {
                            case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                            case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
                        }

                        if (type == RecordType.Unknown)
                        {
                            log(string.Format("мимо"));
                            continue;
                        }

                        var date = ParseDate(cells[2]);
                        switch (type)
                        {
                            case RecordType.Day:
                                date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                break;
                            case RecordType.Hour:
                                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                break;
                        }
                        log(string.Format("дата {0:dd.MM.yy HH:mm:ss}", date));
                        int pos = -1;
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "", date));
                        ++pos;//дата
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[++pos]), "Bar", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[++pos]), "°C", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.CMP, ParseFloat(cells[++pos]), "", date));
                        if (cells.Length == 20)
                        {
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.dpTe, ParseFloat(cells[++pos]), "", date));
                            records.Add(MakeDayOrHourRecord(typeStr, Glossary.T2Tek, ParseFloat(cells[++pos]), "", date));
                        }
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.St2, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.St4, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.St7, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.St6, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.StSy, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));
                    }
                    else if (devType == DevType.TC210 || devType == DevType.TC215)
                    {
                        log(string.Format("ТС21Х парсинг {0}", row));
                        //(13484)(2014-05-28,11:00:00)(4945.2514)(4945.2514)(4955.0000)(4955.0000)(28.16)(104.3250)(1.0017)(0)(0)(0)(0x8104)(CRC Ok)
                        var evt = cells[cells.Length - 2];

                        var type = RecordType.Unknown;
                        var typeStr = "";
                        var vnr = 0;
                        switch (evt)
                        {
                            case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                            case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
                        }

                        if (type == RecordType.Unknown)
                        {
                            continue;
                        }

                        var date = ParseDate(cells[1]);
                        switch (type)
                        {
                            case RecordType.Day:
                                date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                                break;
                            case RecordType.Hour:
                                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                                break;
                        }
                        int pos = 0;
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[++pos]), "", date));
                        //records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "m3", date));
                        ++pos;//дата
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[++pos]), "м³", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[++pos]), "м³", date));

                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[++pos]), "°C", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[++pos]), "Bar", date));

                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[++pos]), "", date));
                        records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));
                    }
                }
                catch (Exception ex)
                {
                    log(string.Format("ошибочка {0}", ex.StackTrace));
                }
            }

            archive.records = records;
            return archive;
        }

        //разделяем сообщения
        private dynamic ParseArchiveResponse(byte[] bytes)
        {
            dynamic archive = new ExpandoObject();            

            if (!bytes.Any())
            {
                archive.badChannel = true;
                archive.success = false;
                archive.error = "не получен ответ на команду";
                return archive;
            }

            if (bytes.Count() < 3)
            {
                archive.badChannel = true;
                archive.success = false;
                archive.error = "слишком короткий ответ на команду";
                return archive;
            }

            archive.badChannel = false;
            archive.success = true;
            archive.error = string.Empty;

            //исключаем начальный STX
            var str = Encoding.ASCII.GetString(bytes, 1, bytes.Length - 1);
            // log(str);
            var rows = new List<string>();

            var strSplits = str.Split(new char[] { (char)STX, (char)ETX });
            foreach (var strSplit in strSplits)
            {
                if (strSplit.Length < 2) continue;
                rows.Add(strSplit);
            }

            archive.rows = rows;
            return archive;
        }

        private dynamic ParseArchiveRecords(List<string> rows, DevType devType, float version, bool isDay = false)
        {
            dynamic archive = new ExpandoObject();
            archive.badChannel = false;
            archive.success = true;
            archive.error = string.Empty;
            archive.isEmpty = false;
            archive.isLock = false;

            var records = new List<dynamic>();

            //var head = string.Format("date\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}",
            //    Glossary.GONo, Glossary.AONo,
            //    Glossary.Vb, Glossary.VbT, Glossary.V, Glossary.Vo,
            //    Glossary.pMP, Glossary.TMP, Glossary.KMP, Glossary.CMP, 
            //    Glossary.dpTe, Glossary.T2Tek, 
            //    Glossary.St2, Glossary.St4, Glossary.St7, Glossary.St6, Glossary.StSy);
            //var head = "date\tGONo\tAONo\tVb\tVbT\tV\tVo\tpMP\tTMP\tKMP\tCMP\tT2Tek\tSt2\tSt4\tSt7\tSt6\tStSy";
            //log(head);

            foreach (var row in rows)
            {
                var cells = row.Replace(")(", "\n").Replace("(", "").Replace(")", "\n").Split('\n');

                var rowRecords = new List<dynamic>();
                
                var crcOk = cells.FirstOrDefault(c => c.Contains("CRC Ok"));
                if (crcOk == null)
                {
                    log(string.Format("парсинг {0}: не содержит контрольную сумму", row), level: 3);
                    continue;
                }

                switch (devType)
                {
                    case DevType.EK260:
                        {
                            //log(string.Format("ЕК260 парсинг {0}", row));
                            var res = ParseCellsArchiveEK260(cells);
                            if (res.success)
                            {
                                rowRecords.AddRange(res.records);
                            }
                            else if (res.badChannel)
                            {
                                return res;
                            }
                            else
                            {
                                log(res.error);
                            }
                        }
                        break;
                    case DevType.EK270:
                        //log(string.Format("ЕК270 парсинг {0}", row));
                        {
                            var res = ParseCellsArchiveEK270(cells);
                            if (res.success)
                            {
                                rowRecords.AddRange(res.records);
                            }
                            else if (res.badChannel)
                            {
                                return res;
                            }
                            else
                            {
                                log(res.error);
                            }
                        }
                        break;
                    case DevType.TC210:
                    case DevType.TC215:
                    case DevType.TC220:
                        //log(string.Format("ТС2хх парсинг {0}", row));
                        {
                            var res = ParseCellsArchiveTC2xx(cells, isDay);
                            if (res.success)
                            {
                                rowRecords.AddRange(res.records);
                            }
                            else if (res.badChannel)
                            {
                                return res;
                            }
                            else
                            {
                                log(res.error);
                            }
                        }
                        break;
                    default:
                        dynamic ar = new ExpandoObject();
                        ar.success = false;
                        ar.badChannel = false;
                        ar.error = string.Format("неподдерживаемый тип прибора {0} вер. {1}", devType, version);
                        return ar;
                }

                records.AddRange(rowRecords);
            }

            if (!records.Any())
            {
                archive.success = false;
                archive.error = "записи не найдены";
                archive.badChannel = false;
            }

            archive.records = records;
            return archive;
        }

        private dynamic ParseAbnormalRecords(List<string> rows)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;

            var records = new List<dynamic>();

            foreach (var row in rows)
            {
                var cells = row.Replace(")(", "\n").Replace("(", "").Replace(")", "\n").Split('\n');
                //var rowRecords = new List<dynamic>();
                if (cells[0].StartsWith("#"))
                {
                    int errcode;
                    if (int.TryParse(cells[0].Substring(1), out errcode))
                    {
                        log(string.Format("парсинг {0}: ошибка {1} - {2}", row, errcode, GetErrorText(errcode)));
                    }
                    else
                    {
                        log(string.Format("парсинг {0}: Неизвестная ошибка", row));
                    }
                    continue;
                }
                //rowRecords.AddRange();
                records.AddRange(ParseCellsAbnormal(cells));
            }

            archive.records = records;
            return archive;
        }

        private dynamic ParseCellsArchiveEK260(string[] cells)
        {
            var records = new List<dynamic>();

            dynamic result = new ExpandoObject();
            result.records = records;
            result.error = string.Empty;
            result.success = false;
            result.badChannel = false;
            //log(string.Format("ParseCellsArchiveEK260 {0}", string.Join(",", cells)));

            var evt = cells[cells.Length - 3];

            var type = RecordType.Hour;
            var typeStr = "Hour";
            var vnr = 0;
            //log(string.Format("evt = {0}", evt));
            switch (evt)
            {
                case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
                default:
                    {
                        // log(string.Format("({0})", string.Join(")(", cells))); break;
                        type = RecordType.Unknown; break;
                    };
            }

            if (type == RecordType.Unknown)
            {
                result.error = string.Format("получен неопознанный тип архива");
                result.badChannel = false;
                return result;
            }

            var date = ParseDate(cells[2]);
            var dateRaw = date;

            switch (type)
            {
                case RecordType.Day:
                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                    break;
                case RecordType.Hour:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                    break;
            }
            
            result.success = true;
            result.date = date;
            result.dateRaw = dateRaw;

            int pos = -1;
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "", date));
            var strdate = cells[++pos];//дата
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[++pos]), "бар", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[++pos]), "°C", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.CMP, ParseFloat(cells[++pos]), "", date));
            if (cells.Length == 20)
            {
                records.Add(MakeDayOrHourRecord(typeStr, Glossary.dpTe, ParseFloat(cells[++pos]), "", date));
                records.Add(MakeDayOrHourRecord(typeStr, Glossary.T2Tek, ParseFloat(cells[++pos]), "", date));
            }

            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St2, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St4, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St7, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St6, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.StSy, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));

            result.type = type;
            return result;
        }

        private dynamic ParseCellsArchiveEK270(string[] cells)
        {
            var records = new List<dynamic>();
            dynamic result = new ExpandoObject();
            result.badChannel = false;
            result.records = records;
            result.success = false;
            result.error = string.Empty;

            //(18484)(640)(2014-05-02,10:00:00)(882066.6309)(884266.5191)(67859)(68044)(12.9437)(10.16)(0.97417)(13.57199)(7)(14)(0)(0)(0)(0x8103)(CRC Ok)
            var evt = cells[cells.Length - 3];

            var type = RecordType.Unknown;
            var typeStr = "";
            //log(string.Format("evt = {0}", evt));
            var vnr = 0;
            switch (evt)
            {
                case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
            }

            if (type == RecordType.Unknown)
            {
                result.error = "мимо";
                result.badChannel = false;
                return result;
            }

            var date = ParseDate(cells[2]);
            var dateRaw = date;
            switch (type)
            {
                case RecordType.Day:
                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                    break;
                case RecordType.Hour:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                    break;
            }

            result.dateRaw = dateRaw;
            result.date = date;
            result.success = true;

            //log(string.Format("дата {0:dd.MM.yy HH:mm:ss}", date));
            int pos = -1;
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "", date));
            ++pos;//дата
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[++pos]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[++pos]), "бар", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[++pos]), "°C", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.CMP, ParseFloat(cells[++pos]), "", date));
            if (cells.Length == 20)
            {
                records.Add(MakeDayOrHourRecord(typeStr, Glossary.dpTe, ParseFloat(cells[++pos]), "", date));
                records.Add(MakeDayOrHourRecord(typeStr, Glossary.T2Tek, ParseFloat(cells[++pos]), "", date));
            }
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St2, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St4, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St7, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.St6, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.StSy, ParseFloat(cells[++pos]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));

            result.type = type;
            return result;
        }

        private dynamic ParseCellsArchiveTC2xx(string[] cells, bool isDay = false)
        {
            var records = new List<dynamic>();

            dynamic result = new ExpandoObject();
            result.badChannel = false;
            result.records = records;
            result.error = string.Empty;
            result.success = false;

            //(13484)(2014-05-28,11:00:00)(4945.2514)(4945.2514)(4955.0000)(4955.0000)(28.16)(104.3250)(1.0017)(0)(0)(0)(0x8104)(CRC Ok)
            var evt = cells[cells.Length - 3];

            var type = RecordType.Unknown;
            var typeStr = "";
            var vnr = 0;
            switch (evt)
            {
                case "0x8103": type = RecordType.Day; typeStr = "Day"; vnr = 24; break;
                case "0x8104": type = RecordType.Hour; typeStr = "Hour"; vnr = 1; break;
            }

            if (isDay)
            {
                type = RecordType.Day; typeStr = "Day";
            }

            if (type == RecordType.Unknown)
            {
                //return records;
                result.error = "мимо";
                result.badChannel = false;
                return result;
            }

            var date = ParseDate(cells[1]);
            var dateRaw = date;
            switch (type)
            {
                case RecordType.Day:
                    date = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddDays(-1);
                    break;
                case RecordType.Hour:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0).AddHours(-1);
                    break;
            }
            
            result.dateRaw = dateRaw;
            result.date = date;
            result.success = true;

            //log(string.Format("ТС значения [{0}]", string.Join(";", cells)));

            int pos = 0;
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.GONo, ParseFloat(cells[pos++]), "", date));
            //records.Add(MakeDayOrHourRecord(typeStr, Glossary.AONo, ParseFloat(cells[++pos]), "м³", date));
            pos++;//дата
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vb, ParseFloat(cells[pos++]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VbT, ParseFloat(cells[pos++]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.V, ParseFloat(cells[pos++]), "м³", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.Vo, ParseFloat(cells[pos++]), "м³", date));

            records.Add(MakeDayOrHourRecord(typeStr, Glossary.TMP, ParseFloat(cells[pos++]), "°C", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.pMP, ParseFloat(cells[pos++]), "бар", date));

            records.Add(MakeDayOrHourRecord(typeStr, Glossary.KMP, ParseFloat(cells[pos++]), "", date));
            records.Add(MakeDayOrHourRecord(typeStr, Glossary.VNR, vnr, "ч", date));

            result.type = type;
            return result;
        }

        private List<dynamic> ParseCellsAbnormal(string[] cells)
        {
            var records = new List<dynamic>();

            var date = ParseDate(cells[2]);
            var evtHexStr = cells[3];

            var evt = Hex2DecL(evtHexStr.Substring(2));
            var abnormalText = GetAbnormalText(evt);

            if (abnormalText != "")
            {
                log(string.Format("{0:dd.MM.yyyy HH:mm:ss} - 0x{1:X4} - {2}", date, evt, abnormalText));
                records.Add(MakeAbnormalRecord((int)(evt >> 16), abnormalText, 0, date));
            }

            return records;
        }
    }
}
