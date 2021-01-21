using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Driver.Ultramag
{
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var parameters = (IDictionary<string, object>)arg;

            byte na = 0x00;
            byte pass = 0x00;

            if (!parameters.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
            {
                log(string.Format("отсутствутют сведения о сетевом адресе"));
                return MakeResult(202, "сетевой адрес");
            }
            else
                log(string.Format("используется сетевой адрес {0}", na));

            if (parameters.ContainsKey("start") && arg.start is DateTime)
            {
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }

            if (parameters.ContainsKey("end") && arg.end is DateTime)
            {
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }

            var components = "Hour;Day;Constant;Abnormal;Current";
            if (parameters.ContainsKey("components"))
            {
                components = arg.components;
                log(string.Format("указаны архивы {0}", components));
            }
            else
                log(string.Format("архивы не указаны, будут опрошены все"));

            switch (what.ToLower())
            {
                case "all": return All(na, components);
            }

            log(string.Format("неопознаная команда {0}", what));
            return MakeResult(201, what);
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        private dynamic All(byte na, string components)
        {
            try
            {

                //1 текущие
                //дата
                var dateResp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x0001, 6)));
                if (!dateResp.success)
                {
                    log(string.Format("дата не получена: {0}", dateResp.error));
                    return MakeResult(0);
                }
                byte[] bytes = dateResp.body;

                Func<byte[], int, byte[]> r = (arr, i) => arr.Skip(i).Take(2).Reverse().ToArray();
                Func<byte[], int, byte[]> r4 = (arr, i) => arr.Skip(i).Take(4).Reverse().ToArray();
                Func<byte[], int, byte[]> r8 = (arr, i) => arr.Skip(i).Take(8).Reverse().ToArray();

                var day = BitConverter.ToInt16(r(bytes, 1), 0);
                var month = BitConverter.ToInt16(r(bytes, 3), 0);
                var year = BitConverter.ToInt16(r(bytes, 5), 0);
                var hour = BitConverter.ToInt16(r(bytes, 7), 0);
                var minute = BitConverter.ToInt16(r(bytes, 9), 0);
                var second = BitConverter.ToInt16(r(bytes, 11), 0);
                var date = new DateTime(year, month, day, hour, minute, second);
                log(string.Format("текущая дата {0:dd.MM.yyyy HH:mm:ss}", date));

                var currents = new List<dynamic>();

                var part1Resp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x000f, 32)));
                if (!part1Resp.success)
                {
                    log(string.Format("текущие не получены: {0}", part1Resp.error));
                    return MakeResult(102);
                }
                byte[] p1bytes = part1Resp.body;

                currents.Add(MakeCurrentRecord("Qw", BitConverter.ToDouble(r8(p1bytes, 1), 0), "м³", date));
                currents.Add(MakeCurrentRecord("Qn", BitConverter.ToDouble(r8(p1bytes, 9), 0), "м³", date));
                currents.Add(MakeCurrentRecord("P", BitConverter.ToDouble(r8(p1bytes, 17), 0), "кПа", date));
                currents.Add(MakeCurrentRecord("T", BitConverter.ToDouble(r8(p1bytes, 25), 0), "°C", date));

                var part2Resp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x003f, 32)));
                if (!part2Resp.success)
                {
                    log(string.Format("текущие не получены: {0}", part2Resp.error));
                    return MakeResult(102);
                }
                byte[] p2bytes = part2Resp.body;
                currents.Add(MakeCurrentRecord("C", BitConverter.ToDouble(r8(p2bytes, 1), 0), "", date));
                currents.Add(MakeCurrentRecord("Vn", BitConverter.ToDouble(r8(p2bytes, 9), 0), "м³", date));
                currents.Add(MakeCurrentRecord("Vw", BitConverter.ToDouble(r8(p2bytes, 17), 0), "м³", date));

                records(currents);

                if (components.Contains("Constant"))
                {
                    var constants = new List<dynamic>();

                    var cnstResp1 = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x005f, 76)));
                    if (!cnstResp1.success)
                    {
                        log(string.Format("константы не получены: {0}", cnstResp1.error));
                        return MakeResult(103);
                    }
                    byte[] cnstBytes = cnstResp1.body;

                    constants.Add(MakeConstantRecord("Плотность газа", BitConverter.ToDouble(r8(cnstBytes, 1), 0), date));
                    constants.Add(MakeConstantRecord("CO2", BitConverter.ToDouble(r8(cnstBytes, 9), 0), date));
                    constants.Add(MakeConstantRecord("N2", BitConverter.ToDouble(r8(cnstBytes, 17), 0), date));
                    constants.Add(MakeConstantRecord("T подст", BitConverter.ToDouble(r8(cnstBytes, 25), 0), date));
                    constants.Add(MakeConstantRecord("Q max подст", BitConverter.ToDouble(r8(cnstBytes, 33), 0), date));
                    constants.Add(MakeConstantRecord("Q min подст", BitConverter.ToDouble(r8(cnstBytes, 41), 0), date));
                    constants.Add(MakeConstantRecord("Qw max", BitConverter.ToDouble(r8(cnstBytes, 49), 0), date));
                    constants.Add(MakeConstantRecord("Qw min", BitConverter.ToDouble(r8(cnstBytes, 57), 0), date));
                    constants.Add(MakeConstantRecord("P max подст", BitConverter.ToDouble(r8(cnstBytes, 65), 0), date));
                    constants.Add(MakeConstantRecord("P min подст", BitConverter.ToDouble(r8(cnstBytes, 73), 0), date));
                    constants.Add(MakeConstantRecord("P атм", BitConverter.ToDouble(r8(cnstBytes, 81), 0), date));
                    //---
                    constants.Add(MakeConstantRecord("Контрактный день", BitConverter.ToInt16(r(cnstBytes, 89), 0), date));
                    constants.Add(MakeConstantRecord("Контрактный час", BitConverter.ToInt16(r(cnstBytes, 91), 0), date));
                    constants.Add(MakeConstantRecord("Подстановочное значение по расходу (0–по стандартному, 1–по рабочему)", BitConverter.ToInt16(r(cnstBytes, 93), 0), date));
                    constants.Add(MakeConstantRecord("Количество вмешательств в параметры ультразвукового преобразователя расхода", BitConverter.ToInt16(r(cnstBytes, 95), 0), date));
                    constants.Add(MakeConstantRecord("Период измерений (сек.)", BitConverter.ToInt16(r(cnstBytes, 97), 0), date));
                    constants.Add(MakeConstantRecord("Время индикации (сек.)", BitConverter.ToInt16(r(cnstBytes, 99), 0), date));

                    //var cnstResp3 = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x0259, 25)));
                    //if (!cnstResp3.success)
                    //{
                    //    log(string.Format("константы не получены: {0}", cnstResp3.error));
                    //    return MakeResult(0);
                    //}
                    //byte[] cnstBytes3 = cnstResp3.body;

                    //constants.Add(MakeConstantRecord("Название предприятия", Encoding.GetEncoding(866).GetString(cnstBytes3, 1, 30), date));
                    //constants.Add(MakeConstantRecord("Номер прибора", Encoding.GetEncoding(866).GetString(cnstBytes3, 31, 10), date));
                    //constants.Add(MakeConstantRecord("Номер датчика давления газа", Encoding.GetEncoding(866).GetString(cnstBytes3, 41, 10), date));
                    //constants.Add(MakeConstantRecord("Номер датчика температуры газа", Encoding.GetEncoding(866).GetString(cnstBytes3, 51, 10), date));


                    //var cnstResp2 = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x00f5, 2)));
                    //if (!cnstResp2.success)
                    //{
                    //    log(string.Format("константы не получены: {0}", cnstResp2.error));
                    //    return MakeResult(0);
                    //}
                    //byte[] cnstBytes2 = cnstResp2.body;
                    //constants.Add(MakeConstantRecord("Сетевой адрес прибора", BitConverter.ToInt16(r(cnstBytes2, 1), 0), date));
                    //constants.Add(MakeConstantRecord("Корректировка хода часов (сек.)", BitConverter.ToInt16(r(cnstBytes2, 3), 0), date));

                    log(string.Format("константы получены"));
                    records(constants);
                }

                //сутки
                if (components.Contains("Day"))
                {
                    //записали номер архива
                    SendWithCrc(MakeWriteRequest(na, 0x012f, new byte[] { 2 }));

                    var days = new List<dynamic>();
                    var ind = (int)(date - getStartDate("day")).TotalDays;
                    log(string.Format("будет прочитано {0} записей", ind));
                    for (var i = ind; i > 0; i--)
                    {
                        if (cancel()) return MakeResult(200);

                        //номер записи
                        SendWithCrc(MakeWriteRequest(na, 0x0131, new byte[] { (byte)i }));
                        //читаем
                        var dayResp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x0141, 18)));
                        if (!dayResp.success)
                        {
                            log(string.Format("ошибка при чтении записи #{0}, {1}", i, dayResp.error));
                            return MakeResult(104);
                        }
                        byte[] dayBytes = dayResp.body;
                        var dday = dayBytes[1];
                        var dmon = dayBytes[2];
                        var dyear = dayBytes[3];
                        var dhour = dayBytes[4];

                        var ddate = new DateTime(2000 + dyear, dmon, dday, dhour, 0, 0);

                        days.Add(MakeDayRecord("P", BitConverter.ToSingle(r4(dayBytes, 5), 0), "кПа", ddate));
                        days.Add(MakeDayRecord("T", BitConverter.ToSingle(r4(dayBytes, 9), 0), "°C", ddate));
                        days.Add(MakeDayRecord("Qw", BitConverter.ToSingle(r4(dayBytes, 13), 0), "м³", ddate));
                        days.Add(MakeDayRecord("Qn", BitConverter.ToSingle(r4(dayBytes, 17), 0), "м³", ddate));
                        days.Add(MakeDayRecord("Vw", BitConverter.ToDouble(r8(dayBytes, 21), 0), "м³", ddate));
                        days.Add(MakeDayRecord("Vn", BitConverter.ToDouble(r8(dayBytes, 29), 0), "м³", ddate));
                        log(string.Format("сутки за {0:dd.MM.yyyy HH:mm:ss} прочитаны", ddate));
                        records(days);
                    }
                }

                if (components.Contains("Hour"))
                {
                    if (cancel()) return MakeResult(200);

                    //записали номер архива
                    SendWithCrc(MakeWriteRequest(na, 0x012f, new byte[] { 1 }));

                    var days = new List<dynamic>();
                    var ind = (int)(date - getStartDate("hour")).TotalHours;
                    for (var i = ind; i > 0; i--)
                    {
                        //номер записи
                        SendWithCrc(MakeWriteRequest(na, 0x0131, new byte[] { (byte)i }));
                        //читаем
                        var dayResp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x0141, 14)));
                        if (!dayResp.success)
                        {
                            log(string.Format("ошибка при чтении записи #{0}, {1}", i, dayResp.error));
                            return MakeResult(105);
                        }
                        byte[] dayBytes = dayResp.body;
                        var dday = dayBytes[1];
                        var dmon = dayBytes[2];
                        var dyear = dayBytes[3];
                        var dhour = dayBytes[4];

                        var ddate = new DateTime(2000 + dyear, dmon, dday, dhour, 0, 0);

                        days.Add(MakeHourRecord("P", BitConverter.ToSingle(r4(dayBytes, 5), 0), "кПа", ddate));
                        days.Add(MakeHourRecord("T", BitConverter.ToSingle(r4(dayBytes, 9), 0), "°C", ddate));
                        days.Add(MakeHourRecord("Vw", BitConverter.ToDouble(r8(dayBytes, 13), 0), "м³", ddate));
                        days.Add(MakeHourRecord("Vn", BitConverter.ToDouble(r8(dayBytes, 21), 0), "м³", ddate));
                        log(string.Format("часы за {0:dd.MM.yyyy HH:mm:ss} прочитаны", ddate));
                        records(days);
                    }
                }

                if (components.Contains("Abnormal"))
                {
                    if (cancel()) return MakeResult(200);

                    //записали номер архива
                    SendWithCrc(MakeWriteRequest(na, 0x012f, new byte[] { 4 }));

                    //var ind = (int)(date - getStartDate("hour")).TotalHours;
                    //log(string.Format("ind={0}", ind));
                    var ddate = DateTime.MinValue;
                    var stDate = getStartDate("Abnormal");
                    byte ind = 1;
                    do
                    {
                        SendWithCrc(MakeWriteRequest(na, 0x0131, new byte[] { ind }));

                        var dayResp = ParseModbusResponse(SendWithCrc(MakeReadRequest(na, 0x0141, 8)));
                        if (!dayResp.success)
                        {
                            log(string.Format("ошибка при чтении записи #{0}, {1}", ind, dayResp.error));
                            return MakeResult(106);
                        }
                        byte[] dayBytes = dayResp.body;

                        var dday = dayBytes[1];
                        var dmon = dayBytes[2];
                        var dyear = dayBytes[3];
                        var dhour = dayBytes[4];
                        var dminute = dayBytes[5];
                        var dsecond = dayBytes[6];

                        var code = dayBytes[7];
                        var isStart = dayBytes[8] == 1;

                        var value = Math.Round(BitConverter.ToDouble(r8(dayBytes, 9), 0), 2);

                        ddate = new DateTime(2000 + dyear, dmon, dday, dhour, dminute, dsecond);

                        ind++;
                        records(new dynamic[] { MakeAbnormalRecord(string.Format("{0}, значение {1}, {2}", AbnormalByCode(code), value, (isStart ? "начало" : "окончание")), 0, ddate) });

                        log(string.Format("нс код {0}, расход {1}, дата {2:dd.MM.yyyy HH:mm:ss}", code, value, ddate));
                    } while (ddate > stDate);
                }
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка: {0}", ex.Message));
                return MakeResult(999);
            }
            return MakeResult(0);
        }

        private string AbnormalByCode(byte code)
        {
            switch (code)
            {
                case 1: return "Измеренное значение рабочего расхода  меньше нижней границы рабочего диапазона измерения счетчика.";
                case 2: return "Измеренное значение рабочего расхода больше верхней границы рабочего диапазона измерения счетчика.";
                case 3: return "Значение рабочего расхода равно 0";
                case 4: return "Отказ канала измерения давления.";
                case 5: return "Измеренное значение давления меньше нижней границы рабочего диапазона измерения.";
                case 6: return "Измеренное значение давления больше верхней границы рабочего диапазона измерения.";
                case 7: return "Отказ канала измерения температуры газа.";
                case 8: return "Измеренное значение температуры газа меньше нижней границы рабочего диапазона измерения.";
                case 9: return "Измеренное значение температуры газа больше верхней границы рабочего диапазона измерения.";
                case 10: return "Отказ канала измерения расхода.";
                case 11: return "Заряд внутреннего источника питания ниже нормы (Необходимо произвести замену внутреннего источника питания!)";
                case 12: return "Отказ канала измерения расхода (десинхронизация УЗПР).";
                case 13: return "Нарушения в работе электроники.";
                case 14:
                case 16:
                case 15: return "Зарезервировано.";
                default: return code.ToString();
            }
        }

        private int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }
    }
}
