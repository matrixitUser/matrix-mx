using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetModbusCurrents(byte na)
        {
            dynamic current = new ExpandoObject();
            current.records = new List<dynamic>();

            var ans = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0x8010, 2)));
            if (!ans.success) return ans;

            var seconds = BitConverter.ToUInt32(ans.register, 0);
            current.date = new DateTime(1970, 1, 1).AddSeconds(seconds);

            //единицы изм.
            var ans2 = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0x012b, 2)));
            if (!ans2.success) return ans2;

            var units = new string[4];
            var pu = GetCurrentUnit(ans2.registers[0]);
            var qu = GetCurrentUnit(ans2.registers[1]);
            var vu = GetCurrentUnit(ans2.registers[2]);
            var mu = GetCurrentUnit(ans2.registers[3]);

            //ЦИФРЫ
            var ans3 = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0xc002, 23)));
            if (!ans3.success) return ans2;

            var offset = 0;

            current.records.Add(MakeCurrentRecord(Glossary.Qt + "1", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qt + "2", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qt + "3", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qt + "4", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));

            current.records.Add(MakeCurrentRecord(Glossary.dQt + "1", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.dQt + "3", BitConverter.ToSingle(ans3.register, offset += 4), qu, current.date));

            current.records.Add(MakeCurrentRecord(Glossary.Wt + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Wt + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Wt + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Wt + "4", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.T + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.T + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.T + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.T + "4", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.Qo + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qo + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qo + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qo + "4", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.Qm + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qm + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qm + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Qm + "4", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.Gm + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Gm + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Gm + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.Gm + "4", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.dGm + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.dGm + "3", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.P + "1", BitConverter.ToSingle(ans3.register, offset += 4), pu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.P + "2", BitConverter.ToSingle(ans3.register, offset += 4), pu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.P + "3", BitConverter.ToSingle(ans3.register, offset += 4), pu, current.date));
            current.records.Add(MakeCurrentRecord(Glossary.P + "4", BitConverter.ToSingle(ans3.register, offset += 4), pu, current.date));

            current.records.Add(MakeCurrentRecord(Glossary.ts + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.tm + "1", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));
            current.records.Add(MakeCurrentRecord(Glossary.tm + "2", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            current.records.Add(MakeCurrentRecord(Glossary.T + "5", BitConverter.ToSingle(ans3.register, offset += 4), "", current.date));

            return current;
        }

        private string GetCurrentUnit(byte code)
        {
            switch (code)
            {
                case 0x11: return "кгс/см2";
                case 0x12: return "кгс/м2";
                case 0x13: return "МПа";
                case 0x14: return "кПа";
                case 0x15: return "мм.рт.ст.";

                case 0x31: return "м³";
                case 0x32: return "тыс.м³";
                case 0x33: return "л";

                case 0x41: return "кг";
                case 0x42: return "т";

                case 0x81: return "кг/ч";
                case 0x82: return "т/ч";
                case 0x83: return "г/сек";

                case 0x91: return "г/сек";
                case 0x92: return "г/сек";
                case 0x93: return "г/сек";
                case 0x94: return "г/сек";
                default: return "";
            }
        }

        private dynamic GetPassportModbus(byte na)
        {
            var regChannelAns = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0x4201, 0x1e)));
            if (!regChannelAns.success) return regChannelAns;
            var regChannel = regChannelAns.register;

            var regUnitAns = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0x4221, 0x1e)));
            if (!regUnitAns.success) return regUnitAns;
            var regUnit = regUnitAns.register;

            var regUnitExtAns = ParseModbusResponse3(Send(MakeModbusRequest3(na, 0x012b, 0x1e)));
            if (!regUnitExtAns.success) return regUnitExtAns;
            var regUnitExt = regUnitExtAns.register;

            var passport = ParseModbusPasport(regChannel, regUnit);
            log(string.Format("Паспорт прочтен успешно. Параметров={0} Ед.изм={1}", passport.channels.Count(), passport.units.Count()));

            return passport;
        }

        private dynamic ParseModbusPasport(byte[] regChannel, byte[] regUnit)
        {
            dynamic passport = new ExpandoObject();
            passport.units = new List<dynamic>();
            for (var i = 0; i < regUnit.Length; i += 2)
            {
                var unit = GetModbusUnit((byte)((regUnit[i + 0] << 8) + regUnit[i + 1]));
                //passport.units.Add(new Unit(regUnit, i));
                passport.units.Add(unit);
            }

            passport.channels = new List<dynamic>();
            for (var i = 0; i < regChannel.Length; i += 2)
            {
                passport.channels.Add(ParseModbusChannel(regChannel, i));
            }
            return passport;
        }

        private dynamic ParseModbusChannel(byte[] data, int offset)
        {
            dynamic channel = new ExpandoObject();
            channel.name = GetModbusParameterName(data[offset]);
            channel.number = data[offset + 1];
            return channel;
        }

        private string GetModbusUnit(byte code)
        {
            switch (code)
            {
                /* Используются в каналах типа 
       Давление (P), Давление абс. (Pa), Давл.барометрич. (Pb), Перепад давл. (dP) */
                case 0x11: return "кгс/см²";
                case 0x12: return "кгс/м²";
                case 0x13: return "МПа";
                case 0x14: return "кПа";
                case 0x15: return "мм.рт.ст.";

                /* Кол-во тепла (Qt), Разн. кол-ва тепла (dQt),
                Суточный pасход на р/ч (Gr) */
                case 0x21: return "Гкал";
                case 0x22: return "ГДж";
                case 0x23: return "МДж";

                /* Тепл.мощность (Wt) */
                case 0x27: return "Гкал/час";
                case 0x28: return "ГДж/час";
                case 0x29: return "МДж/час";

                /* Температура (T), Разность температур (dT) */
                case 0x2D: return "°C";

                /* Объем (G0), Разн. объемов (dGo), Объем рабочий (Gw), Разн.объемов раб. (dGw), Суточный pасход на р/ч (Gr)  */
                case 0x31: return "м³";
                case 0x32: return "тыс.м³";
                case 0x33: return "литр";

                /* Объем нормальн. (Gn), Разн. объемов норм. (dGn), Суточный pасход на р/ч (Gr) */
                case 0x37: return "н.м³";
                case 0x38: return "тыс.н.м³";

                // Масса (Gm), Разн. масс (dGm), Суточный pасход на р/ч (Gr) 
                case 0x41: return "кг";
                case 0x42: return "тонн";

                // Плотность (Ro), Плотн. при НУ (Ron) 
                case 0x51: return "кг/м³";
                case 0x52: return "г/см³";
                case 0x53: return "т/м³";

                // Высота (H), Уровень (L), Смещение (dL) 
                case 0x61: return "м";
                case 0x62: return "см";
                case 0x63: return "мм";
                case 0x64: return "км";
                case 0x65: return "-";

                // Скорость (V), Вибрация (Vb) 
                case 0x68: return "м/сек";
                case 0x69: return "cм/сек";
                case 0x6A: return "мм/сек";
                case 0x6B: return "км/час";

                // Напряжение (U) 
                case 0x71: return "В";
                case 0x72: return "мВ";
                case 0x73: return "кВ";

                // Ток (I) 
                case 0x76: return "мА";
                case 0x77: return "А";
                case 0x78: return "кА";

                // Сопротивление (R) 
                case 0x7B: return "Ом";

                // Частота (F) 
                case 0x7D: return "Гц";
                case 0x7E: return "кГц";

                // Расход массовый (Qm)  
                case 0x81: return "кг/час";
                case 0x82: return "тонн/час";
                case 0x83: return "г/сек";

                // Расход объемный (Qo), Расход рабочий (Qw) 
                case 0x91: return "м³/час";
                case 0x92: return "тыс.м³/час";
                case 0x93: return "л/сек";
                case 0x94: return "м³/мин";
                case 0x95: return "л/мин";

                // Расход норм.об. (Qn) 
                case 0x97: return "н.м³/час";
                case 0x98: return "тыс.н.м³/час";
                case 0x99: return "н.м³/мин";

                // Дебит скважины (Qd) 
                case 0x9C: return "м³/сут";
                case 0x9D: return "тыс.м³/сут";

                // Работа узла (tm), Время наработки (ts) 
                case 0xA2: return "час:мин";

                // Содерж. воды (Me), Влажность (Fi) 
                case 0xA5: return "проц.";

                // Электр.энергия (Ge), Разн. электр. энерг. (dGe), Суточный pасход на р/ч (Gr) 
                case 0xB1: return "кВт∙час";
                case 0xB2: return "Вт∙час";
                case 0xB3: return "MВт∙час";

                // Эл. мощность (N), Мех. мощность (Nm) 
                case 0xB7: return "кВт";
                case 0xB8: return "Вт";
                case 0xB9: return "MВт";

                // Момент силы (M) 
                case 0xBD: return "Н∙м";
                case 0xBE: return "кН∙м";
                case 0xBF: return "кгс∙м";

                // Частота вращ. (n) 
                case 0xC3: return "Об/сек";
                case 0xC4: return "Об/мин";
                case 0xC5: return "тыс.об/мин";
                case 0xC6: return "%";

                // Пеpеключатель (Sw), Порядк. ном. (N), Коэфф. сжим. (Kp), Счетчик имп. (Np) 
                default: return ""; //(безразмерн.)
            }
        }

        private string GetModbusParameterName(byte code)
        {
            switch (code)
            {
                case 0x08: return Glossary.T;
                case 0x10: return Glossary.P;
                case 0x18: return Glossary.dP;
                case 0x20: return Glossary.H;
                case 0x28: return Glossary.Qo;
                case 0x30: return Glossary.Go;
                case 0x31: return Glossary.dGo;
                case 0x38: return Glossary.Qm;
                case 0x40: return Glossary.Gm;
                case 0x41: return Glossary.dGm;
                case 0x48: return Glossary.Qn;
                case 0x50: return Glossary.Gn;
                case 0x51: return Glossary.dGn;
                case 0x58: return Glossary.Qt;
                case 0x59: return Glossary.dQt;
                case 0x60: return Glossary.Wt;
                case 0x68: return Glossary.tm;
                case 0x70: return Glossary.Pa;
                case 0x78: return Glossary.ts;
                case 0x80: return Glossary.Sw;
                case 0x88: return Glossary.Pb;
                case 0x90: return Glossary.L;
                case 0x98: return Glossary.NN;
                case 0xA1: return Glossary.Kpr;
                case 0xA8: return Glossary.Qd;
                case 0xB0: return Glossary.Ge;
                case 0xB1: return Glossary.dGe;
                case 0xB2: return Glossary.Np;
                case 0xB8: return Glossary.Gr;
                case 0xB9: return Glossary.Ron;
                case 0xC0: return Glossary.Ro;
                case 0xC2: return Glossary.Vb;
                case 0xC8: return Glossary.Me;
                case 0xC9: return Glossary.Fi;
                case 0xD0: return Glossary.N;
                case 0xD1: return Glossary.Nm;
                case 0xD2: return Glossary.V;
                case 0xD3: return Glossary.dL;
                case 0xD4: return Glossary.G;
                case 0xD5: return Glossary.M;
                case 0xD7: return Glossary.U;
                case 0xD8: return Glossary.I;
                case 0xD9: return Glossary.R;
                case 0xE0: return Glossary.F;
                case 0xE1: return Glossary.n;
                case 0xE8: return Glossary.dT;
                case 0xF0: return Glossary.Qw;
                case 0xF8: return Glossary.Gw;
                case 0xF9: return Glossary.dGw;
                default: return "параметр код " + code.ToString("X2");
            }
        }

        private byte[] MakeModbusRequest(byte na, byte fn, byte[] bytes)
        {
            var data = new List<byte>();
            data.Add(na);
            data.Add(fn);

            data.AddRange(bytes);

            var crc = CalcCrc16Modbus(data.ToArray());
            data.Add(crc[0]);
            data.Add(crc[1]);
            return data.ToArray();
        }

        private byte[] MakeModbusRequest3(byte na, UInt16 start, UInt16 count)
        {
            var bytes = new byte[]
            {
                GetHighByte(start),
                GetLowByte(start),
                GetHighByte(count),
                GetLowByte(count)
            };
            return MakeModbusRequest(na, 0x03, bytes);
        }

        private dynamic ParseModbusResponse(byte[] data)
        {
            dynamic response = new ExpandoObject();

            if (data.Length < 5)
            {
                response.success = false;
                response.error = "в кадре ответа не может содержаться менее 5 байт";
                return response;
            }

            if (!CheckCrc16Modbus(data))
            {
                response.success = false;
                response.error = "контрольная сумма кадра не сошлась";
                return response;
            }

            response.na = data[0];
            response.fn = data[1];
            response.body = data.Skip(2).Take(data.Length - (2 + 2)).ToArray();

            //modbus error
            if (response.fn > 0xc1)
            {
                var exceptionCode = data[2];
                response.success = false;
                response.error = string.Format("устройство вернуло ошибку: {0:X}", exceptionCode);
                return response;
            }

            response.success = true;
            return response;
        }

        private dynamic ParseModbusResponse3(byte[] data)
        {
            var response = ParseModbusResponse(data);
            if (!response.success) return response;

            response.length = response.body[0];
            if (response.body.Length < (response.length + 1))
            {
                response.success = false;
                response.error = "пакет короток";
                return response;
            }

            response.register = response.body.Skip(1).ToArray();

            return response;
        }
    }
}
