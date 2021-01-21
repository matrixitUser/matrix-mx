using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetConstant(byte na, List<byte> channels, short password, int version, DateTime date)
        {
            dynamic сonstant = new ExpandoObject();
            сonstant.success = false;
            сonstant.error = string.Format("версия {0} не поддерживается", version);

            var registers = new Dictionary<short, short>();
            if ((version >= 300 && version <= 399) ||
                (version >= 400 && version <= 499) ||
                (version == 655))
            {
                registers.Add(0x00E6, 128);
                registers.Add(0x00E7, 128);
            }

            if (version >= 500 && version <= 599)
            {
                registers.Add(0x003C, 128);
                registers.Add(0x003D, 128);
            }

            if ((version >= 609 && version <= 619) ||
                (version >= 620 && version <= 629) ||
                (version >= 850 && version <= 899))
            {
                if (channels.Contains(1)) { registers.Add(0x005C, 132); registers.Add(0x005D, 124); };
                if (channels.Contains(2)) { registers.Add(0x0060, 132); registers.Add(0x0061, 124); };
                if (channels.Contains(3)) { registers.Add(0x0064, 132); registers.Add(0x0065, 124); };
                if (channels.Contains(4)) { registers.Add(0x0068, 132); registers.Add(0x0069, 124); };
            }

            if (version >= 950 && version <= 969)
            {
                registers.Add(0x003C, 128);
                registers.Add(0x003D, 128);
                registers.Add(0x003E, 128);
                registers.Add(0x003F, 128);
                registers.Add(0x0040, 128);
                registers.Add(0x0041, 128);
                registers.Add(0x0042, 128);
                registers.Add(0x0043, 128);
            }

            if (version >= 970 && version <= 999)
            {
                if (channels.Contains(1)) { registers.Add(0x0017, 512); registers.Add(0x001A, 512); };
                if (channels.Contains(2)) { registers.Add(0x001B, 512); registers.Add(0x001E, 512); };
                if (channels.Contains(3)) { registers.Add(0x001F, 512); registers.Add(0x0022, 512); };
                if (channels.Contains(4)) { registers.Add(0x0023, 512); registers.Add(0x0026, 512); };
            }

            dynamic calledstop = new ExpandoObject();
            calledstop.error = "вызвана остановка процесса";
            calledstop.success = false;

            List<byte> buffer = new List<byte>();
            foreach (var register in registers)
            {
                if (cancel()) return calledstop;
                var writeBytes = Make16Request(na, RVS, new short[] { register.Key });

                var writeAnswer = ParseModbusResponse(SendWithCrc(Make16Request(na, RVS, new short[] { register.Key })));
                if (!writeAnswer.success) return writeAnswer;

                if (cancel()) return calledstop;
                var readBytes1 = Make3Request(na, 0x0000, 0x0040);
                var readAnswer1 = ParseModbusResponse(SendWithCrc(readBytes1));
                if (!readAnswer1.success) return readAnswer1;

                byte[] bytes1 = readAnswer1.body;
                buffer.AddRange(InvertBytes(bytes1.Skip(1)));

                if (cancel()) return calledstop;
                var readBytes2 = Make3Request(na, 0x0040, 0x0040);
                var readAnswer2 = ParseModbusResponse(SendWithCrc(readBytes2));
                if (!readAnswer2.success) return readAnswer2;

                byte[] bytes2 = readAnswer2.body;
                buffer.AddRange(InvertBytes(bytes2.Skip(1)));

                if (cancel()) return calledstop;
                var readBytes3 = Make3Request(na, 0x0080, 0x0004);
                var readAnswer3 = ParseModbusResponse(SendWithCrc(readBytes3));
                if (!readAnswer3.success) return readAnswer3;

                byte[] bytes3 = readAnswer3.body;
                buffer.AddRange(InvertBytes(bytes3.Skip(1)));
            }

            if (!buffer.Any())
                return сonstant;


            /// Адреса данных ПП версий регистратора 400..499, 609..619, 620..629, 655, 850..899
            if ((version >= 400 && version <= 499) ||
                (version >= 609 && version <= 619) ||
                (version >= 620 && version <= 629) ||
                (version == 655) ||
                (version >= 850 && version <= 899))
            {
                return ParseConstant1Response(buffer.ToArray(), channels, date);
            }

            if (version >= 950 && version <= 999)
            {
                return ParseConstant2Response(buffer.ToArray(), channels, date);
            }

            return сonstant;
        }

        private IEnumerable<byte> InvertBytes(IEnumerable<byte> data)
        {
            var bytes = data.ToArray();
            var buffer = new List<byte>();
            //инвертируем байты
            for (int i = 0; i < bytes.Length - 1; i += 2)
            {
                buffer.Add(bytes[i + 1]);
                buffer.Add(bytes[i]);
            }
            return buffer;
        }


        /// <summary>
        /// Адреса данных ПП версий регистратора 400..499, 609..619, 620..629, 655, 850..899
        /// </summary>
        private dynamic ParseConstant1Response(byte[] bytes, List<byte> channels, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.error = string.Empty;
            constant.success = true;

            constant.records = new List<dynamic>();

            foreach (var ch in channels)
            {
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, заводской номер ПП", ch), BitConverter.ToInt16(bytes, 0), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, диаметр ПП при 20°C, мм", ch), BitConverter.ToSingle(bytes, 16), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, диаметр тела обтекания при 20°C, мм", ch), BitConverter.ToSingle(bytes, 32), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, договорная температура, °K", ch), (float)BitConverter.ToUInt16(bytes, 112) / 100f, date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, договорное давление, кПа", ch), BitConverter.ToSingle(bytes, 114), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, минимальная граница расхода при н.у., н.м³/ч", ch), BitConverter.ToSingle(bytes, 120), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, максимальная граница расхода при р.у., н.м³/ч", ch), BitConverter.ToSingle(bytes, 124), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, минимальная граница температуры, °K", ch), BitConverter.ToSingle(bytes, 128), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, максимальная граница температуры, °K", ch), BitConverter.ToSingle(bytes, 132), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, минимальная граница давления, кПа", ch), BitConverter.ToSingle(bytes, 136), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, максимальная граница давления, кПа", ch), BitConverter.ToSingle(bytes, 140), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, плотность среды при н.у., кг/м³", ch), BitConverter.ToSingle(bytes, 252), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, газовая постоянная", ch), BitConverter.ToSingle(bytes, 256), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, коэффициент адиабаты", ch), BitConverter.ToSingle(bytes, 260), date));
                //  constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, среда", ch), GetEnvironment(BitConverter.ToUInt16(bytes, 448)), date));

                for (int i = 0; i < 12; i++)
                {
                    var x = bytes[450 + i];
                    if (x < 1 || x > 36) continue;
                    constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, {1}, %", ch, GetComponent1(x)), BitConverter.ToSingle(bytes, 462 + i * 4), date));
                }
            }

            return constant;
        }

        /// <summary>
        /// Адреса данных ПП версий регистратора 950..999
        /// </summary>
        private dynamic ParseConstant2Response(byte[] bytes, List<byte> channels, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.error = string.Empty;
            constant.success = true;

            constant.records = new List<dynamic>();

            foreach (var ch in channels)
            {
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, заводской номер ПП", ch), BitConverter.ToUInt16(bytes, 356), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, минимальная граница температуры, °K", ch), BitConverter.ToSingle(bytes, 436), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, максимальная граница температуры, °K", ch), BitConverter.ToSingle(bytes, 440), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, минимальная граница давления, кПа", ch), BitConverter.ToSingle(bytes, 444), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, максимальная граница давления, кПа", ch), BitConverter.ToSingle(bytes, 448), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, договорная температура, °K", ch), BitConverter.ToSingle(bytes, 696), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, договораное давление, кПа", ch), BitConverter.ToSingle(bytes, 700), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, среда", ch), GetEnvironment2(BitConverter.ToUInt16(bytes, 744)), date));

                ushort type = BitConverter.ToUInt16(bytes, 746);
                var value = string.Empty;
                switch (type)
                {
                    case 0: value = "объемный"; break;
                    case 1: value = "массовый"; break;
                    case 2: value = "молярный"; break;
                }
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, тип процентного состава", ch), value, date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, плотность среды при стандартных условиях, кг/м³", ch), BitConverter.ToSingle(bytes, 748), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, влагосодержание (процент H₂0)", ch), BitConverter.ToSingle(bytes, 752), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, массовое теплосодержание, Мдж/м³", ch), BitConverter.ToSingle(bytes, 756), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, температура возвратной среды, °C", ch), BitConverter.ToSingle(bytes, 764), date));

                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, условный диаметр ПП, м", ch), BitConverter.ToSingle(bytes, 1408), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, условный диаметр ПП между датчиками, м", ch), BitConverter.ToSingle(bytes, 1412), date));
                constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, условный путь луча, м", ch), BitConverter.ToSingle(bytes, 1416), date));


                for (int i = 0; i < 13; i++)
                {
                    constant.records.Add(MakeConstantRecord(string.Format("ПП{0}, {1}, %", ch, GetComponent2(i)), BitConverter.ToSingle(bytes, 768 + i * 4), date));
                }
            }

            return constant;
        }

        private string GetEnvironment1(ushort index)
        {
            switch (index)
            {
                case 1: return "воздух";
                case 2: return "природный газ";
                case 3: return "попутный газ";
                case 4: return "диоксид углерода";
                case 5: return "азот";
                case 6: return "нефтяной газ";
                case 7: return "коксовый газ";
                case 8: return "аргон";
                case 9: return "водяной пар";
                case 10: return "вода";
                case 11: return "этан";
                case 12: return "пропан";
                case 13: return "бутан";
                case 14: return "ацетилен";
                case 15: return "этилен";
                case 16: return "состав пользователя";
                default: return "неопределенно";
            }
        }

        private string GetEnvironment2(ushort index)
        {
            switch (index)
            {
                case 0: return "неопределенно";
                case 1: return "газ природный. Метод расчета NX-19";
                case 2: return "газ Природный. Метод расета GERG91";
                case 3: return "газ нефтяной, попутный, коксовый. Метод расчета ВНИЦ СМВ";
                case 4: return "газ нефтяной, попутный, коксовый. Метод ГСССД";
                case 5: return "сжиженный углеводородный газ";
                case 6: return "воздух";
                case 7: return "азот";
                case 8: return "углекислый газ";
                case 9: return "аргон";
                case 10: return "водяной пар";
                case 11: return "этан";
                case 12: return "аммиак";
                case 13: return "ацетилен";
                case 14: return "этилен";
                case 15: return "кислород";
                case 16: return "водород";
                case 17: return "гелий";
                case 18: return "вода";
                case 19: return "состав пользователя";
                default: return "неопределенно";
            }
        }

        private string GetComponent1(byte index)
        {
            switch (index)
            {
                case 1: return "диоксид углерода (CO₂)";
                case 2: return "азот (N₂)";
                case 3: return "метан (CH₄)";
                case 4: return "этан (C₂H₆)";
                case 5: return "пропан (C₃H₈)";
                case 6: return "и-Бутан (и-C₄H₁₀)";
                case 7: return "н-Бутан (н-C₄H₁₀)";
                case 8: return "и-Пентан (и-C₅H₁₂)";
                case 9: return "н-Пентан (н-C₅H₁₂)";
                case 10: return "гексан (C₆H₁₄)";
                case 11: return "кислород (O₂)";
                case 12: return "сероводород (H₂S)";
                case 13: return "гептан (C₇H₁₆)";
                case 14: return "октан (C₈H₁₈)";
                case 15: return "нонан (C₉H₂₀)";
                case 16: return "декан (C₁₀H₂₂)";
                case 17: return "ацетилен (C₂H₂)";
                case 18: return "этилен (C₂H₄)";
                case 19: return "пропилен (C₃H₆)";
                case 20: return "бензол (C₆H₆)";
                case 21: return "толуол (C₇H₈)";
                case 22: return "водород (H₂)";
                case 23: return "водяной пар (H₂O)";
                case 24: return "аммиак (H₃N)";
                case 25: return "метанол (CH₄O)";
                case 26: return "диоксид серы (SO₂)";
                case 27: return "гелий (He)";
                case 28: return "неон (Ne)";
                case 29: return "аргон (Ar)";
                case 30: return "монооксид углерода (CO)";
                case 31: return "метилмеркаптан (CH₄S)";
                case 32: return "этилмеркаптан";
                case 33: return "пропилмеркаптан";
                case 34: return "бутилмеркаптан";
                case 35: return "сероуглерод";
                case 36: return "сероокись углерода";
                default: return "неопределенно";
            }
        }

        private string GetComponent2(int index)
        {
            switch (index)
            {
                case 0: return "Азот (N₂)";
                case 1: return "Диоксид углерода (CO₂)";
                case 2: return "Сероводород (H₂S)";
                case 3: return "Метан (CH₄)";
                case 4: return "Этан (C₂H₈)";
                case 5: return "Пропан (C₃H₈)";
                case 6: return "н-Бутан (н-C₄H₁₀)";
                case 7: return "и-Бутан (и-C₄H₁₀)";
                case 8: return "н-Пентан (н-C₅H₁₂)";
                case 9: return "и-Пентан (н-C₅H₁₂)";
                case 10: return "Гексан (н-C₆H₁₄)";
                case 11: return "Гептан (н-C₇H₁₆)";
                case 12: return "н-октан (н-C₈H₁₈)";
            }
            return "Резерв";
        }

        private dynamic MakeConstantRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}
