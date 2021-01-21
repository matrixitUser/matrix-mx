using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        private dynamic GetConstant(byte na)
        {
            dynamic constant = new ExpandoObject();

            var date = DateTime.Now;
            List<dynamic> records = new List<dynamic>();

            //

            dynamic serviceOptions = GetServiceOptions(na, date);
            if (!serviceOptions.success) return serviceOptions;
            constant.contractHour = serviceOptions.contractHour;
            records.AddRange(serviceOptions.records);

            //

            dynamic OptionsOnChannels = GetOptionsOnChannels(na, date);
            if (!OptionsOnChannels.success) return OptionsOnChannels;

            //

            records.AddRange(OptionsOnChannels.records);
            constant.units = OptionsOnChannels.units;
            dynamic DescriptionSensors = GetDescriptionSensors(na, date, serviceOptions.code);
            if (!DescriptionSensors.success) return DescriptionSensors;
            records.AddRange(DescriptionSensors.records);

            //

            constant.records = records;
            constant.success = true;
            return constant;
        }

        private dynamic GetPages(byte na, int startIndex, int count)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;

            List<byte[]> pages = new List<byte[]>();
            int errorCount = 0;

            ////ПО ОДНОЙ
            //for (int index = startIndex; index < startIndex + count; index++)
            //{
            //    dynamic page = null;
            //    for (int i = 0; i < TRY_COUNT; i++)
            //    {
            //        page = ReadFlash(na, (short)(index), 1, parameters);
            //        if (page.success) break;
            //        errorCount++;
            //    }

            //    //dynamic page = SendFlash(MakeFlashRequest(na, (short)(index), 1), 1, parameters);
            //    if (!page.success)
            //    {
            //        log(string.Format("ошибка при чтении flash-карты: {0}", page.error));
            //        return page;
            //    }

            //    pages = pages.Union(page.body as List<byte[]>).ToList();
            //}

            //НЕСКОЛЬКО СТРАНИЦ СРАЗУ
            dynamic flash = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                flash = ReadFlash(na, (short)(startIndex), (byte)count);
                if (flash.success) break;
                errorCount++;
            }

            if (!flash.success)
            {
                log(string.Format("ошибка при чтении flash-карты: {0}", flash.error));
                return flash;
            }

            if (count != flash.n)
            {
                answer.error = string.Format("ошибка при чтении flash-карты: несовпадение количества прочтенных страниц ({0}) и ожидаемых ({1})", flash.n, count);
            }

            pages = flash.body;

            log(string.Format("прочитано {0} страниц flash, начиная с {1}{2}", count, startIndex, errorCount > 0 ? string.Format(" ({0} повторений)", errorCount) : ""));

            answer.success = true;
            answer.body = pages;
            answer.n = pages.Count;
            return answer;
        }

        private dynamic GetServiceOptions(byte na, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;

            var pages = GetPages(na, 8 + 0, 6);
            if (!pages.success) return pages;

            List<dynamic> records = new List<dynamic>();

            var page00 = (pages.body[0] as IEnumerable<byte>);

            var param00 = ParseParameter(page00.Skip(0).Take(16).ToArray(), false);
            records.Add(MakeConstantRecord("схема учета (потребления) газа (СП)", param00.body.value, date));
            constant.code = param00.body.value;
            var param01 = ParseParameter(page00.Skip(16).Take(16).ToArray(), false);
            records.Add(MakeConstantRecord("период опроса датчиков (ПИ), с", param01.body.value, date));
            var param02 = ParseParameter(page00.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord("сетевой номер прибора (ИТ), с", param02.body.value, date));
            var param03 = ParseParameter(page00.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord("идентификатор прибора для внешнего устройства (ИД)", param03.body.value, date));

            var page01 = (pages.body[1] as IEnumerable<byte>);

            var param04 = ParseParameter(page01.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("константа барометрического давления (Pk), мм.рт.ст", param04.body.value, date));
            var param05 = ParseParameter(page01.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("плотность сухого природного (rc), газа кг/м³", param05.body.value, date));
            var param06 = ParseParameter(page01.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord("молярная доля азота в природном газе (Xa)", Math.Round(param06.body.value, 5), date));
            var param07 = ParseParameter(page01.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord("молярная доля углеводорода в природном газе (Xy)", param07.body.value, date));

            var page02 = (pages.body[2] as IEnumerable<byte>);

            var param08 = ParseParameter(page02.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("относительное влагосодержание в газе (rв)", param08.body.value, date));
            var param09 = ParseParameter(page02.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("календарь. Дата пуска (ДО)", string.Format("{0:00}-{1:00}-{2:00}", param09.body.data[2], param09.body.data[1], param09.body.data[0]), date));
            var param10 = ParseParameter(page02.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord("время (ТО)", string.Format("{0:00}-{1:00}-{2:00}", param10.body.data[2], param10.body.data[1], param10.body.data[0]), date));
            var param11 = ParseParameter(page02.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord("коррекция хода часов (КС)", param11.body.value, date));

            var page03 = (pages.body[3] as IEnumerable<byte>);

            var param12 = ParseParameter(page03.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("день перехода на зимнее время (ДЗ)", string.Format("{0:00}-{1:00}", param12.body.data[0], param12.body.data[1]), date));
            var param13 = ParseParameter(page03.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("день перехода на летнее время (ДЛ)", string.Format("{0:00}-{1:00}", param13.body.data[0], param13.body.data[1]), date));
            var param14 = ParseParameter(page03.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord("расчетные сутки (РС)", param14.body.data[0], date));
            var param15 = ParseParameter(page03.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord("расчетный час (ЧР)", param15.body.data[0], date));
            constant.contractHour = param15.body.data[0];

            var page04 = (pages.body[4] as IEnumerable<byte>);

            var param16 = ParseParameter(page04.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("Вкл/Выкл автоматической печати суточных отчетов (ПС)", param16.body.value, date));
            var param17 = ParseParameter(page04.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("Вкл/Выкл автоматической печати декадных отчетов (ПД)", param17.body.value, date));
            var param18 = ParseParameter(page04.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord("Вкл/Выкл автоматической печати месячных отчетов (ПМ)", param18.body.value, date));
            var param19 = ParseParameter(page04.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord("суточная норма поставки газа (Vд)", param19.body.value, date));

            var page05 = (pages.body[5] as IEnumerable<byte>);

            var param20 = ParseParameter(page05.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("вес импульса для сигнала ДОЗА м³ (ЦД)", param20.body.value, date));

            constant.records = records;
            return constant;
        }

        private dynamic GetOptionsOnChannels(byte na, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;

            //var pages = ReadFlash(na, 8 + 12, 6);
            var pages = GetPages(na, 8 + 12, 6);
            if (!pages.success) return pages;

            List<dynamic> records = new List<dynamic>();
            Dictionary<string, string> units = new Dictionary<string, string>();
            constant.units = units;

            var page12 = (pages.body[0] as IEnumerable<byte>);
            var page13 = (pages.body[1] as IEnumerable<byte>);
            var page14 = (pages.body[2] as IEnumerable<byte>);
            var page15 = (pages.body[3] as IEnumerable<byte>);
            var page18 = (pages.body[4] as IEnumerable<byte>);
            var page19 = (pages.body[5] as IEnumerable<byte>);


            #region param 54-55

            //Размерность Р1 и P1к (МПа, кПа, кгс/см2 , кгс/м2 )
            var param54 = ParseParameter(page13.Skip(32).Take(16).ToArray());
            units.Add("Р1", GetUnit(param54.body.data[0]));
            units.Add("P1к", GetUnit(param54.body.data[0]));
            //Размерность ∆P1 и ∆P1к (МПа, кПа, кгс/см2, кгс/м2 )
            var param55 = ParseParameter(page13.Skip(48).Take(16).ToArray());
            units.Add("dP1", GetUnit(param55.body.data[0]));
            units.Add("dP1к", GetUnit(param55.body.data[0]));
            #endregion

            #region param 62-63

            //Размерность Р2 и P2к (МПа, кПа, кгс/см2 , кгс/м2 )
            var param62 = ParseParameter(page15.Skip(32).Take(16).ToArray());
            units.Add("Р2", GetUnit(param62.body.data[0]));
            units.Add("Р2k", GetUnit(param62.body.data[0]));

            //Размерность ∆P2 и ∆P2к (МПа, кПа, кгс/см2, кгс/м2 )
            var param63 = ParseParameter(page15.Skip(48).Take(16).ToArray());
            units.Add("dР2", GetUnit(param63.body.data[0]));
            units.Add("dР2k", GetUnit(param63.body.data[0]));

            #endregion

            #region param 74-79

            //Размерность ∆Р3 (МПа, кПа, кгс/см2 , кгс/м2 )
            var param74 = ParseParameter(page18.Skip(32).Take(16).ToArray());
            units.Add("dР3", GetUnit(param74.body.data[0]));
            //Размерность Рб (МПа, кПа, кгс/см2, кгс/м2 )
            var param75 = ParseParameter(page18.Skip(48).Take(16).ToArray());
            units.Add("Рb", GetUnit(param75.body.data[0]));

            //Размерность Р3 (МПа, кПа, кгс/см2 , кгс/м2 )
            var param76 = ParseParameter(page19.Skip(0).Take(16).ToArray());
            units.Add("Р3", GetUnit(param76.body.data[0]));

            //Размерность Р4 (МПа, кПа, кгс/см2 , кгс/м2 )
            var param77 = ParseParameter(page19.Skip(16).Take(16).ToArray());
            units.Add("Р4", GetUnit(param77.body.data[0]));

            #endregion

            #region param 50-53

            var param50 = ParseParameter(page12.Skip(32).Take(16).ToArray());
            records.Add(MakeConstantRecord(string.Format("константа давления газа (P1к), {0}", constant.units["P1к"]), param50.body.value, date));

            var param51 = ParseParameter(page12.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord(string.Format("договорное значение перепада давления (dP1к), {0}", constant.units["dP1к"]), param51.body.value, date));

            var param52 = ParseParameter(page13.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("константа температуры газа (t1k), °C", param52.body.value, date));

            var param53 = ParseParameter(page13.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("константа объемного расхода в рабочих условиях (Qp1k)", param53.body.value, date));
            #endregion

            #region param 58-61

            var param58 = ParseParameter(page14.Skip(32).Take(16).ToArray());

            records.Add(MakeConstantRecord(string.Format("константа давления газа (Р2k), {0}", constant.units["Р2k"]), param58.body.value, date));
            var param59 = ParseParameter(page14.Skip(48).Take(16).ToArray());
            records.Add(MakeConstantRecord(string.Format("договорное значение перепада давления (dР2k), {0}", constant.units["dР2k"]), param59.body.value, date));

            var param60 = ParseParameter(page15.Skip(0).Take(16).ToArray());
            records.Add(MakeConstantRecord("константа температуры газа (t2k), °C", param60.body.value, date));

            var param61 = ParseParameter(page15.Skip(16).Take(16).ToArray());
            records.Add(MakeConstantRecord("константа объемного расхода в рабочих условиях (Qp2k)", param61.body.value, date));

            #endregion

            foreach (var key in units.Keys)
            {
                records.Add(MakeConstantRecord(string.Format("единица измерения {0}", key), units[key], date));
            }

            constant.records = records;
            return constant;
        }

        private dynamic GetDescriptionSensors(byte na, DateTime date, int code)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            var sequenceSensors = GetSequenceSensors(code);
            byte count = (byte)Math.Round((sequenceSensors.Length * 11) / 4f, 0);
            //var pages = ReadFlash(na, 8 + 25, count);
            var pages = GetPages(na, 8 + 25, count);
            if (!pages.success) return pages;

            List<dynamic> records = new List<dynamic>();

            List<byte[]> parameters = new List<byte[]>();
            foreach (var page in pages.body)
            {
                var p = (page as IEnumerable<byte>);
                parameters.Add(p.Skip(0).Take(16).ToArray());
                parameters.Add(p.Skip(16).Take(16).ToArray());
                parameters.Add(p.Skip(32).Take(16).ToArray());
                parameters.Add(p.Skip(48).Take(16).ToArray());
            }

            for (int i = 0; i < sequenceSensors.Length; i++)
            {
                var group = parameters.Skip(i * 11).Take(11).ToArray();
                var VD = ParseParameter(group[0], false);
                if (VD.body.value == 0) continue;

                switch (sequenceSensors[i])
                {
                    case SensorNames.dP1:
                        {
                            records.Add(MakeConstantRecord("ВД/∆P1", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/∆P1", VP.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/∆P1", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/∆P1", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/∆P1", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/∆P1", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.dP2:
                        {
                            records.Add(MakeConstantRecord("ВД/∆P2", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/∆P2", VP.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/∆P2", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/∆P2", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/∆P2", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/∆P2", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.dP3:
                        {
                            records.Add(MakeConstantRecord("ВД/∆P3", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/∆P3", VP.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/∆P3", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/∆P3", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/∆P3", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/∆P3", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.P1:
                        {
                            records.Add(MakeConstantRecord("ВД/P1", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/P1", VP.body.value, date));
                            var KC = ParseParameter(group[5]); records.Add(MakeConstantRecord("КС/P1", KC.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/P1", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/P1", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/P1", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/P1", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.P2:
                        {
                            records.Add(MakeConstantRecord("ВД/P2", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/P2", VP.body.value, date));
                            var KC = ParseParameter(group[5]); records.Add(MakeConstantRecord("КС/P2", KC.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/P2", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/P2", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/P2", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/P2", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.P3:
                        {
                            records.Add(MakeConstantRecord("ВД/P3", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/P3", VP.body.value, date));
                            var KC = ParseParameter(group[5]); records.Add(MakeConstantRecord("КС/P3", KC.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/P3", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/P3", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/P3", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/P3", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.P4:
                        {
                            records.Add(MakeConstantRecord("ВД/P4", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/P4", VP.body.value, date));
                            var KC = ParseParameter(group[5]); records.Add(MakeConstantRecord("КС/P4", KC.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/P4", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/P4", KN.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/P4", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/P4", UN.body.value, date));
                            continue;
                        }
                    case SensorNames.Pб:
                        {
                            records.Add(MakeConstantRecord("ВД/Pб", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/Pб", VP.body.value, date));
                            var KC = ParseParameter(group[5]); records.Add(MakeConstantRecord("КС/Pб", KC.body.value, date));
                            var KV = ParseParameter(group[6]); records.Add(MakeConstantRecord("КВ/Pб", KV.body.value, date));
                            var KN = ParseParameter(group[7]); records.Add(MakeConstantRecord("КН/Pб", KN.body.value, date));
                            continue;
                        }
                    case SensorNames.t1:
                        {
                            records.Add(MakeConstantRecord("ВД/t1", VD.body.value, date));
                            var TD = ParseParameter(group[1], false); records.Add(MakeConstantRecord("ТД/t1", TD.body.value, date));
                            continue;
                        }
                    case SensorNames.t2:
                        {
                            records.Add(MakeConstantRecord("ВД/t2", VD.body.value, date));
                            var TD = ParseParameter(group[1], false); records.Add(MakeConstantRecord("ТД/t2", TD.body.value, date));
                            continue;
                        }
                    case SensorNames.t3:
                        {
                            records.Add(MakeConstantRecord("ВД/t3", VD.body.value, date));
                            var TD = ParseParameter(group[1], false); records.Add(MakeConstantRecord("ТД/t3", TD.body.value, date));
                            continue;
                        }
                    case SensorNames.Qp1:
                        {
                            records.Add(MakeConstantRecord("ВД/Qp1", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/Qp1", VP.body.value, date));
                            var NP = ParseParameter(group[3]); records.Add(MakeConstantRecord("НП/Qp1", NP.body.value, date));
                            var CI = ParseParameter(group[4]); records.Add(MakeConstantRecord("ЦИ/Qp1", CI.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/Qp1", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/Qp1", UN.body.value, date));
                            var VN = ParseParameter(group[10]); records.Add(MakeConstantRecord("Vн/Qp1", VN.body.value, date));
                            continue;
                        }
                    case SensorNames.Qp2:
                        {
                            records.Add(MakeConstantRecord("ВД/Qp2", VD.body.value, date));
                            var VP = ParseParameter(group[2]); records.Add(MakeConstantRecord("ВП/Qp2", VP.body.value, date));
                            var NP = ParseParameter(group[3]); records.Add(MakeConstantRecord("НП/Qp2", NP.body.value, date));
                            var CI = ParseParameter(group[4]); records.Add(MakeConstantRecord("ЦИ/Qp2", CI.body.value, date));
                            var UV = ParseParameter(group[8]); records.Add(MakeConstantRecord("УВ/Qp2", UV.body.value, date));
                            var UN = ParseParameter(group[9]); records.Add(MakeConstantRecord("УН/Qp2", UN.body.value, date));
                            var VN = ParseParameter(group[10]); records.Add(MakeConstantRecord("Vн/Qp2", VN.body.value, date));
                            continue;
                        }
                }
            }

            constant.records = records;
            return constant;
        }

        private string GetUnit(byte code)
        {
            if (units.ContainsKey(code))
                return units[code];

            return string.Empty;
        }

        private SensorNames[] GetSequenceSensors(int code)
        {
            switch (code)
            {
                case 0: return new SensorNames[] { SensorNames.P1, SensorNames.dP3, SensorNames.dP1, SensorNames.Pб, SensorNames.P3, SensorNames.t1, SensorNames.t2, SensorNames.Qp1, SensorNames.Qp2 };
                case 1: return new SensorNames[] { SensorNames.P1, SensorNames.dP3, SensorNames.P2, SensorNames.Pб, SensorNames.P3, SensorNames.t1, SensorNames.t2, SensorNames.Qp1, SensorNames.Qp2 };
                case 2: return new SensorNames[] { SensorNames.P1, SensorNames.dP3, SensorNames.P2, SensorNames.dP1, SensorNames.dP2, SensorNames.t1, SensorNames.t2, SensorNames.Qp1, SensorNames.Qp2 };
                case 3: return new SensorNames[] { SensorNames.P1, SensorNames.dP2, SensorNames.P2, SensorNames.Pб, SensorNames.dP1, SensorNames.t1, SensorNames.t2, SensorNames.Qp1, SensorNames.Qp2 };
                case 4: return new SensorNames[] { SensorNames.P1, SensorNames.dP2, SensorNames.P2, SensorNames.dP1, SensorNames.P3, SensorNames.t1, SensorNames.t2, SensorNames.Qp1, SensorNames.Qp2 };
                case 5: return new SensorNames[] { SensorNames.P1, SensorNames.dP3, SensorNames.dP1, SensorNames.Pб, SensorNames.P3, SensorNames.t1, SensorNames.t3, SensorNames.Qp1 };
                case 6: return new SensorNames[] { SensorNames.P1, SensorNames.dP3, SensorNames.dP1, SensorNames.P3, SensorNames.P4, SensorNames.t1, SensorNames.t3, SensorNames.Qp1 };
                default: return new SensorNames[] { };
            }
        }

        /// <summary>
        /// см. док. стр. 21
        /// </summary>
        private Dictionary<byte, string> units = new Dictionary<byte, string>()
        {
            {0x00,"кПа"},
            {0x01,"МПа"},
            {0x02,"кгс/см²"},
            {0x03,"кгс/м²"}
        };

        private dynamic ParseConstantResponse(byte[] bytes, DateTime date)
        {
            var constant = ParseRamResponse(bytes);
            if (!constant.success) return constant;

            for (int i = 0; i < constant.body.Length; i += 16)
            {
                var data = constant.body.Skip(i).Take(16).ToArray();
                if (data.Length < 16)
                {
                    constant.success = false;
                    constant.error = "число байт в ответе не соответствует необходимому (16 байт)";
                    return constant;
                }

                byte flag = data[0];
                string StringValue = Encoding.GetEncoding(866).GetString(data, 4, 8);
                var Data = data.Skip(12).Take(4).ToArray();
            }

            constant.records = new List<dynamic>();


            return constant;
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

        private dynamic ParseParameter(byte[] bytes, bool isFloat = true)
        {
            dynamic answer = new ExpandoObject();
            dynamic parameter = new ExpandoObject();
            answer.body = parameter;
            if (bytes.Length != 16)
            {
                answer.success = false;
                answer.error = "тело параметра имеет не верное количество байт (16 байт)";
                return answer;
            }
            parameter.flags = bytes[0];
            parameter.data = bytes.Skip(12).Take(4).ToArray();
            if (isFloat)
            {
                parameter.value = Helper.SpgFloatToIEEE(parameter.data, 0);
            }
            else
            {
                parameter.value = BitConverter.ToInt32(parameter.data, 0);
            }
            answer.success = true;
            return answer;
        }
    }

    enum SensorNames
    {
        P1 = 0,
        P2 = 1,
        P3 = 2,
        P4 = 3,
        dP1 = 4,
        dP2 = 5,
        dP3 = 6,
        Pб = 7,
        t1 = 8,
        t2 = 9,
        t3 = 10,
        Qp1 = 11,
        Qp2 = 12
    }
}
