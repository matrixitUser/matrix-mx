using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;
using System.Threading;
using Matrix.SurveyServer.Driver.Common.Crc;


namespace Matrix.Poll.Driver.WB_MAP12E
{
    /// <summary>
    /// Драйвер для электросчетчика WIREN BOARD - 12MAP12E
    /// </summary>
    public partial class Driver
    {
        //int hourlyStart = 30;

        double UrmsL1;
        double UrmsL2;
        double UrmsL3;

        double[] Ch1TrmsArr = new double[3];
        double[] Ch2TrmsArr = new double[3];
        double[] Ch3TrmsArr = new double[3];
        double[] Ch4TrmsArr = new double[3];

        double[] KoefPower1 = new double[3];
        double[] KoefPower2 = new double[3];
        double[] KoefPower3 = new double[3];
        double[] KoefPower4 = new double[3];

        private class Block
        {
            public DateTime Date { get; set; }
            public byte Number { get; set; }
        }

        UInt32? NetworkAddress = null;
        Int32[] CoeffCh1 = new Int32[3]; // кол-во витков токовых трансформатора на канале 1;
        Int32[] CoeffCh2 = new Int32[3]; // кол-во витков токовых трансформатора на канале 2;
        Int32[] CoeffCh3 = new Int32[3]; // кол-во витков токовых трансформатора на канале 3;
        Int32[] CoeffCh4 = new Int32[3]; // кол-во витков токовых трансформатора на канале 4;

        Int32[] PhaseCh1 = new Int32[3]; // фазовый сдвиг токовых трансформатора на канале 1;
        Int32[] PhaseCh2 = new Int32[3]; // фазовый сдвиг токовых трансформатора на канале 2;
        Int32[] PhaseCh3 = new Int32[3]; // фазовый сдвиг токовых трансформатора на канале 3;
        Int32[] PhaseCh4 = new Int32[3]; // фазовый сдвиг токовых трансформатора на канале 4;

        bool[] editChannels = {false, false, false, false};
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private byte GetNaPacket()
        {
            if (NetworkAddress == null)
            {
                return 0;
            }
            byte result = (byte)(NetworkAddress.Value);
            return result;
        }

        private byte[] MakePackage(byte cmd0, byte cmd1, byte cmd2, byte cmd3, byte cmd4)
        {
            var result = new List<byte>();

            result.Add(GetNaPacket());

            result.Add(cmd0);
            result.Add(cmd1);
            result.Add(cmd2);
            result.Add(cmd3);
            result.Add(cmd4);

            var crc = new Crc16Modbus();
            result.AddRange(crc.Calculate(result.ToArray(), 0, result.Count).CrcData);

            return result.ToArray();
        }

        private byte[] MakePackage(byte cmd1, byte cmd2, byte cmd3, byte cmd4)
        {
            return MakePackage(0x04, cmd1, cmd2, cmd3, cmd4);
        }

        #region Корректировка трансформаторов тока
        private void GetCoeff(byte cmd1, byte cmd2, int channel, string phase)
        {
            dynamic dt = Send(MakePackage(cmd1, cmd2, 0x00, 0x01), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Коэффициент тока для канала {channel} фазы {phase}");
                return;
            }
            else
            {
                double coeff = dt.Body[0]*256 + dt.Body[1];
                log($"Количество витков для канала {channel} {phase}: {coeff}");
            }
        }
        private void GetPhase(byte cmd1, byte cmd2, int channel, string phase)
        {
            dynamic dt = Send(MakePackage(cmd1, cmd2, 0x00, 0x01), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Фазовый сдвиг тока для канала {channel} фазы {phase}");
                return;
            }
            else
            {
                byte[] tmpBytes = new byte[] { dt.Body[1], dt.Body[0] };
                double coeff = dt.Body[0] * 256 + dt.Body[1];
                log($"Фазовый сдвиг для канала {channel} {phase}: {coeff}");
            }
        }

        private void SetCoeff(byte cmd1, byte cmd2, Int32 coef, int channel, string phase)
        {
            byte[] bytes = BitConverter.GetBytes(coef);
            dynamic dt = Send(MakePackage(0x06, cmd1, cmd2, bytes[1], bytes[0]), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось Установить:Коэффициент тока для канала {channel} фазы {phase}");
                return;
            }
            else
            {
                //if (dt.Body[1] == bytes[1] && dt.Body[2] == bytes[0])
                //{
                //    log($"Успех: Коэффициент тока для канала {channel} фазы {phase}");
                //}
                //else log($"Не удалось Установить:Коэффициент тока для канала {channel} фазы {phase}");
            }
        }

        private void SetPhase(byte cmd1, byte cmd2, Int32 coef, int channel, string phase)
        {
            byte[] bytes = BitConverter.GetBytes(coef);
            dynamic dt = Send(MakePackage(0x06, cmd1, cmd2, bytes[1], bytes[0]), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось Установить: Сдвиг по фазе для канала {channel} фазы {phase}");
                return;
            }
            else
            {
                //if (dt.Body[1] == bytes[1] && dt.Body[2] == bytes[0])
                //{
                //    log($"Успех: Сдвиг по фазе для канала {channel} фазы {phase}");
                //}
                //else log($"Не установилось: Сдвиг по фазе для канала {channel} фазы {phase}");
            }
        }
        #endregion

        #region константы
        private double[] GetPowerCoeff(byte cmd, int ch)
        {
            double[] KoefP = new double[3];
            dynamic dt = Send(MakePackage(cmd, 0xbd, 0x00, 0x03), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Коэффициент мощности для фазы L1 для канала 2");
                return KoefP;
            }
            else
            {

                byte[] U1 = new byte[] { dt.Body[1], dt.Body[0] };
                byte[] U2 = new byte[] { dt.Body[3], dt.Body[2] };
                byte[] U3 = new byte[] { dt.Body[5], dt.Body[4] };
                KoefP[0] = BitConverter.ToInt16(U1, 0) * 0.001;
                KoefP[1] = BitConverter.ToInt16(U2, 0) * 0.001;
                KoefP[2] = BitConverter.ToInt16(U3, 0) * 0.001;
                log($"Коэффициент мощности для фазы L1 для канала {ch}: {KoefP[0]}");
                log($"Коэффициент мощности для фазы L2 для канала {ch}: {KoefP[1]}");
                log($"Коэффициент мощности для фазы L3 для канала {ch}: {KoefP[2]}");
                //answer.records.Add(MakeCurrentRecord("Коэффициент мощности для фазы L1 для канала 2", UrmsL1, "", dateNow));
                //answer.records.Add(MakeCurrentRecord("Напряжение(RMS) на фазе L2", UrmsL2, "вольт", dateNow));
                //answer.records.Add(MakeCurrentRecord("Напряжение(RMS) на фазе L3", UrmsL3, "вольт", dateNow));
                return KoefP;
            }
        }

        private dynamic GetTotalEnergy(byte cmd, int ch, DateTime dateNow) // 0x1200, 0x2200, 0x3200, 0x4200
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x00, 0x00, 0x04), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Суммарная прямая активная энергия для канала {ch}");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                double UrmsL1 = BitConverter.ToUInt32(dt.Body, 0) * 0.00001;

                log($"Суммарная прямая активная энергия для канала {ch}: {UrmsL1} кВт*ч");

                answer.records.Add(MakeCurrentRecord($"Суммарная прямая активная энергия для канала {ch}", UrmsL1, "кВт*ч", dateNow));

                return answer;
            }
        }
        #endregion

        #region текущие
        private dynamic GetVoltage(DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(0x14, 0x10, 0x00, 0x06), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Напряжение на трех фазах");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();
                
                byte[] U1 = new byte[] { dt.Body[3], dt.Body[2], dt.Body[1], dt.Body[0] };
                byte[] U2 = new byte[] { dt.Body[7], dt.Body[6], dt.Body[5], dt.Body[4] };
                byte[] U3 = new byte[] { dt.Body[11], dt.Body[10], dt.Body[9], dt.Body[8] };
                UrmsL1 = BitConverter.ToUInt32(U1, 0) * 1.52588 * 0.0000001;
                UrmsL2 = BitConverter.ToUInt32(U2, 0) * 1.52588 * 0.0000001;
                UrmsL3 = BitConverter.ToUInt32(U3, 0) * 1.52588 * 0.0000001;
                log($"Напряжение (RMS) на фазе L1: {UrmsL1:N3} вольт");
                log($"Напряжение (RMS) на фазе L2: {UrmsL2:N3} вольт");
                log($"Напряжение (RMS) на фазе L3: {UrmsL3:N3} вольт");
                answer.records.Add(MakeCurrentRecord("Напряжение(RMS) на фазе L1", UrmsL1, "вольт", dateNow));
                answer.records.Add(MakeCurrentRecord("Напряжение(RMS) на фазе L2", UrmsL2, "вольт", dateNow));
                answer.records.Add(MakeCurrentRecord("Напряжение(RMS) на фазе L3", UrmsL3, "вольт", dateNow));
                return answer;
            }
        }

        private dynamic GetAmper(byte cmd, int channel, DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x16, 0x00, 0x06), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: токи на канале {channel}");
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                UInt32 Ch1TrmsL1i2 = (UInt32)(dt.Body[0] * 0x1000000 + dt.Body[1] * 0x10000 + dt.Body[2] * 0x100 + dt.Body[3]);
                UInt32 Ch1TrmsL2i2 = (UInt32)(dt.Body[4] * 0x1000000 + dt.Body[5] * 0x10000 + dt.Body[6] * 0x100 + dt.Body[7]);
                UInt32 Ch1TrmsL3i2 = (UInt32)(dt.Body[8] * 0x1000000 + dt.Body[9] * 0x10000 + dt.Body[10] * 0x100 + dt.Body[11]);

                double Ch1TrmsL1 = Ch1TrmsL1i2 * 2.44141 * 0.0000001;
                double Ch1TrmsL2 = Ch1TrmsL2i2 * 2.44141 * 0.0000001;
                double Ch1TrmsL3 = Ch1TrmsL3i2 * 2.44141 * 0.0000001;

                if(channel == 1)
                {
                    log($"Ток (RMS) на ТРК1 L1: {Ch1TrmsL1:N3} ампер");
                    log($"Ток (RMS) на ТРК1 L2: {Ch1TrmsL2:N3} ампер");
                    log($"Ток (RMS) на ТРК1 L3: {Ch1TrmsL3:N3} ампер");

                    log($"Ток (RMS) на фазе L1 для канала {channel} ТРК1: {Ch1TrmsL1:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L2 для канала {channel} ТРК1: {Ch1TrmsL2:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L3 для канала {channel} ТРК1: {Ch1TrmsL3:N3} ампер", 3);
                }
                if (channel == 2)
                {
                    log($"Ток (RMS) на ТРК2 L1: {Ch1TrmsL1:N3} ампер");
                    log($"Ток (RMS) на ТРК2 L2: {Ch1TrmsL2:N3} ампер");
                    log($"Ток (RMS) на ТРК2 L3: {Ch1TrmsL3:N3} ампер");

                    log($"Ток (RMS) на фазе L1 для канала {channel} ТРК2: {Ch1TrmsL1:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L2 для канала {channel} ТРК2: {Ch1TrmsL2:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L3 для канала {channel} ТРК2: {Ch1TrmsL3:N3} ампер", 3);
                }
                if (channel == 3)
                {
                    //log($"Ток (RMS) на ТРК1Э L1: {Ch1TrmsL1:N3} ампер");
                    log($"Ток (RMS) на ТРК1Э L2: {Ch1TrmsL2:N3} ампер");
                    log($"Ток (RMS) на ТРК2Э L3: {Ch1TrmsL3:N3} ампер");

                    //log($"Ток (RMS) на фазе L1 для канала {channel} ТРК2: {Ch1TrmsL1:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L2 для канала {channel} ТРК1Э: {Ch1TrmsL2:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L3 для канала {channel} ТРК2Э: {Ch1TrmsL3:N3} ампер", 3);
                }
                if (channel == 4)
                {
                    log($"Ток (RMS) на АПТ1 L1: {Ch1TrmsL1:N3} ампер");
                    log($"Ток (RMS) на АПТ1 L2: {Ch1TrmsL2:N3} ампер");
                    log($"Ток (RMS) на АПТ1 L3: {Ch1TrmsL3:N3} ампер");

                    log($"Ток (RMS) на фазе L1 для канала {channel} АПТ1: {Ch1TrmsL1:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L2 для канала {channel} АПТ1: {Ch1TrmsL2:N3} ампер", 3);
                    log($"Ток (RMS) на фазе L3 для канала {channel} АПТ1: {Ch1TrmsL3:N3} ампер", 3);
                }


                answer.toque1 = Ch1TrmsL1;
                answer.toque2 = Ch1TrmsL2;
                answer.toque3 = Ch1TrmsL3;

                answer.records.Add(MakeCurrentRecord($"Ток (RMS) на фазе L1 для канала {channel}", Ch1TrmsL1, "ампер", dateNow));
                answer.records.Add(MakeCurrentRecord($"Ток (RMS) на фазе L2 для канала {channel}", Ch1TrmsL2, "ампер", dateNow));
                answer.records.Add(MakeCurrentRecord($"Ток (RMS) на фазе L3 для канала {channel}", Ch1TrmsL3, "ампер", dateNow));

            }
            return answer;
        }

        public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
        {
            return source.Skip(start).Take(count).Reverse().ToArray();
        }

        private dynamic GetPower(byte cmd, int channel, DateTime dateNow, double[] toque) // 1302
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x02, 0x00, 0x06), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: активная мощность на канале {channel}");
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();


                Int32 PL1i = (Int32)(dt.Body[0] * 0x1000000 + dt.Body[1] * 0x10000 + dt.Body[2] * 0x100 + dt.Body[3]);
                Int32 PL3iTmp = BitConverter.ToInt32(Reverse(dt.Body, 8, 4), 0);
                Int32 PL2i = (Int32)(dt.Body[4] * 0x1000000 + dt.Body[5] * 0x10000 + dt.Body[6] * 0x100 + dt.Body[7]);
                log($"{dt.Body[8]:x16}, {dt.Body[9]:x16}, {dt.Body[10]:x16}, {dt.Body[11]:x16}  BitConverter: {PL3iTmp} | {PL3iTmp * 0.00512}");
                Int32 PL3i = (Int32)(dt.Body[8] * 0x1000000 + dt.Body[9] * 0x10000 + dt.Body[10] * 0x100 + dt.Body[11]);

                double PL1 = PL1i * 0.00512;
                double PL2 = PL2i * 0.00512;
                double PL3 = PL3i * 0.00512;

                double PL1res = toque[0] * UrmsL1;
                double PL2res = toque[1] * UrmsL2;
                double PL3res = toque[2] * UrmsL3;

                double dev1 = PL1 / PL1res;
                double dev2 = PL2 / PL2res;
                double dev3 = PL3 / PL3res;

                if (channel == 1)
                {
                    //log($"Активная мощность для ТРК1 L1: {PL1:N3} Вт | вычисляемая {PL1res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК1 L2: {PL2:N3} Вт | вычисляемая {PL2res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК1 L3: {PL3:N3} Вт | вычисляемая {PL3res:N3} Вт", 3);

                    log($"Активная мощность для фазы L1 для канала {channel} ТРК1: {PL1:N3} Вт | выч*коэф {PL1res * KoefPower1[0]:N3} | вычисляемая {PL1res:N3} Вт | отношение {dev1:N3} | коэф {KoefPower1[0]:N3}", 3);
                    log($"Активная мощность для фазы L2 для канала {channel} ТРК1: {PL2:N3} Вт | выч*коэф {PL2res * KoefPower1[1]:N3} | вычисляемая {PL2res:N3} Вт | отношение {dev2:N3} | коэф {KoefPower1[1]:N3}", 3);
                    log($"Активная мощность для фазы L3 для канала {channel} ТРК1: {PL3:N3} Вт | выч*коэф {PL3res * KoefPower1[2]:N3} | вычисляемая {PL3res:N3} Вт | отношение {dev3:N3} | коэф {KoefPower1[2]:N3}", 3);
                }

                if (channel == 2)
                {
                    //log($"Активная мощность для ТРК2 L1: {PL1:N3} Вт | вычисляемая {PL1res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L2: {PL2:N3} Вт | вычисляемая {PL2res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L3: {PL3:N3} Вт | вычисляемая {PL3res:N3} Вт", 3);

                    log($"Активная мощность для фазы L1 для канала {channel} ТРК2: {PL1:N3} Вт |  выч*коэф {PL1res * KoefPower2[0]:N3}  | вычисляемая {PL1res:N3} Вт | отношение {dev1:N3} | коэф {KoefPower2[0]:N3}", 3);
                    log($"Активная мощность для фазы L2 для канала {channel} ТРК2: {PL2:N3} Вт |  выч*коэф {PL2res * KoefPower2[1]:N3}  | вычисляемая {PL2res:N3} Вт | отношение {dev2:N3} | коэф {KoefPower2[1]:N3}", 3);
                    log($"Активная мощность для фазы L3 для канала {channel} ТРК2: {PL3:N3} Вт |  выч*коэф {PL3res * KoefPower2[2]:N3}  | вычисляемая {PL3res:N3} Вт | отношение {dev3:N3} | коэф {KoefPower2[2]:N3}", 3);
                }

                if (channel == 3)
                {
                    //log($"Активная мощность для ТРК2 L1: {PL1:N3} Вт | вычисляемая {PL1res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L2: {PL2:N3} Вт | вычисляемая {PL2res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L3: {PL3:N3} Вт | вычисляемая {PL3res:N3} Вт", 3);

                    //log($"Активная мощность для фазы L1 для канала {channel} ТРК1Э: {PL1:N3} Вт | вычисляемая {PL1res:N3} Вт", 3);
                    log($"Активная мощность для фазы L2 для канала {channel} ТРК1Э: {PL2:N3} Вт |  выч*коэф {PL2res * KoefPower3[1]:N3} | вычисляемая {PL2res:N3} Вт | отношение {dev2:N3} | коэф {KoefPower3[1]:N3}", 3);
                    log($"Активная мощность для фазы L3 для канала {channel} ТРК2Э: {PL3:N3} Вт |  выч*коэф {PL3res * KoefPower3[2]:N3} | вычисляемая {PL3res:N3} Вт | отношение {dev3:N3} | коэф {KoefPower3[2]:N3}", 3);
                }

                if (channel == 4)
                {
                    //log($"Активная мощность для ТРК2 L1: {PL1:N3} Вт | вычисляемая {PL1res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L2: {PL2:N3} Вт | вычисляемая {PL2res:N3} Вт", 3);
                    //log($"Активная мощность для ТРК2 L3: {PL3:N3} Вт | вычисляемая {PL3res:N3} Вт", 3);

                    log($"Активная мощность для фазы L1 для канала {channel} АПТ1: {PL1:N3} Вт |  выч*коэф {PL1res * KoefPower4[0]:N3} | вычисляемая {PL1res:N3} Вт | отношение {dev1:N3} | коэф {KoefPower4[0]:N3}", 3);
                    log($"Активная мощность для фазы L2 для канала {channel} АПТ1: {PL2:N3} Вт |  выч*коэф {PL2res * KoefPower4[1]:N3} | вычисляемая {PL2res:N3} Вт | отношение {dev2:N3} | коэф {KoefPower4[1]:N3}", 3);
                    log($"Активная мощность для фазы L3 для канала {channel} АПТ1: {PL3:N3} Вт |  выч*коэф {PL3res * KoefPower4[2]:N3} | вычисляемая {PL3res:N3} Вт | отношение {dev3:N3} | коэф {KoefPower4[2]:N3}", 3);
                }

                answer.records.Add(MakeCurrentRecord($"Активная мощность для фазы L1 для канала {channel}", PL1, "Вт", dateNow));
                answer.records.Add(MakeCurrentRecord($"Активная мощность для фазы L2 для канала {channel}", PL2, "Вт", dateNow));
                answer.records.Add(MakeCurrentRecord($"Активная мощность для фазы L3 для канала {channel}", PL3, "Вт", dateNow));
            }
            return answer;
        }

        private dynamic GetActivePower(byte cmd, int channel, DateTime dateNow) // 1204
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x04, 0x00, 0x0C), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: активная энергия на канале {channel}");
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                double Ch3APL1 = BitConverter.ToInt64(dt.Body, 0) * 0.00001;
                double Ch3APL2 = BitConverter.ToInt64(dt.Body, 8) * 0.00001;
                double Ch3APL3 = BitConverter.ToInt64(dt.Body, 16) * 0.00001;

                log($"Активная энергия для фазы L1 для канала {channel}: {Ch3APL1} кВт*ч");
                log($"Активная энергия для фазы L2 для канала {channel}: {Ch3APL2} кВт*ч");
                log($"Активная энергия для фазы L3 для канала {channel}: {Ch3APL3} кВт*ч");

                answer.records.Add(MakeCurrentRecord($"Активная энергия для фазы L1 для канала {channel}", Ch3APL1, "кВт*ч", dateNow));
                answer.records.Add(MakeCurrentRecord($"Активная энергия для фазы L2 для канала {channel}", Ch3APL2, "кВт*ч", dateNow));
                answer.records.Add(MakeCurrentRecord($"Активная энергия для фазы L3 для канала {channel}", Ch3APL3, "кВт*ч", dateNow));

            }
            return answer;
        }
        #endregion

        #region суточные
        private dynamic GetMaxVoltage(DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(0x18, 0x10, 0x00, 0x06), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Пиковые значения напряжения на трех фазах");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                if(dt.Body[2] != 0xff && dt.Body[3] != 0xff)
                {
                    double UrmsL1 = BitConverter.ToInt32(dt.Body, 0) * 0.01;
                    log($"Пиковое значение напряжения на фазе L1: {UrmsL1} вольт");
                    answer.records.Add(MakeDayRecord("Пиковое значение напряжения на фазе L1", UrmsL1, "вольт", dateNow));
                }
                else log($"Пиковое значение напряжения на фазе L1: Несоответствие");

                if (dt.Body[6] != 0xff && dt.Body[7] != 0xff)
                {
                    double UrmsL2 = BitConverter.ToInt32(dt.Body, 4) * 0.01;
                    log($"Пиковое значение напряжения на фазе L2: {UrmsL2} вольт");
                    answer.records.Add(MakeDayRecord("Пиковое значение напряжения на фазе L2", UrmsL2, "вольт", dateNow));
                }
                else log($"Пиковое значение напряжения на фазе L2: Несоответствие");

                if (dt.Body[10] != 0xff && dt.Body[11] != 0xff)
                {
                    double UrmsL3 = BitConverter.ToInt32(dt.Body, 8) * 0.01;
                    log($"Пиковое значение напряжения на фазе L3: {UrmsL3} вольт");
                    answer.records.Add(MakeDayRecord("Пиковое значение напряжения на фазе L3", UrmsL3, "вольт", dateNow));
                }
                else log($"Пиковое значение напряжения на фазе L3: Несоответствие");

                return answer;
            }
        }

        private dynamic GetFrequency(DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(0x10, 0xf8, 0x00, 0x02), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Частота");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                byte[] bytes = new byte[] { dt.Body[1], dt.Body[0] };
                double UrmsL1 = BitConverter.ToUInt16(bytes, 0) * 0.01;
                log($"Частота: {UrmsL1} Гц");
                answer.records.Add(MakeDayRecord("Частота", UrmsL1, "Гц", dateNow));
                return answer;
            }
        }

        private dynamic GetMaxToque(byte cmd, int channel, DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x18, 0x00, 0x06), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Пиковые значения тока на канале {channel}");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                if (dt.Body[2] != 0xff || dt.Body[3] != 0xff)
                {
                    double IrmsL1 = BitConverter.ToInt32(dt.Body, 0) * 0.016;
                    log($"Пиковые значения тока на фазе L1 для канала {channel}: {IrmsL1} aмпер");
                    answer.records.Add(MakeDayRecord($"Пиковые значения тока на фазе L1 для канала {channel}", IrmsL1, "Ампер", dateNow));
                }
                else log($"Пиковые значения тока на фазе L1 для канала {channel}: Несоответствие");

                if (dt.Body[2] != 0xff || dt.Body[3] != 0xff)
                {
                    double IrmsL2 = BitConverter.ToInt32(dt.Body, 4) * 0.016;
                    log($"Пиковое значение тока на фазе L2 для канала {channel}: {IrmsL2} aмпер");
                    answer.records.Add(MakeDayRecord($"Пиковое значение тока на фазе L2 для канала {channel}", IrmsL2, "Ампер", dateNow));
                }
                else log($"Пиковые значения тока на фазе L2 для канала {channel}: Несоответствие");

                if (dt.Body[2] != 0xff || dt.Body[3] != 0xff)
                {
                    double IrmsL3 = BitConverter.ToInt32(dt.Body, 8) * 0.016;
                    log($"Пиковое значение тока на фазе L3 для канала {channel}: {IrmsL3} aмпер");
                    answer.records.Add(MakeDayRecord($"Пиковое значение тока на фазе L3 для канала {channel}", IrmsL3, "Ампер", dateNow));
                }
                else log($"Пиковые значения тока на фазе L3 для канала {channel}: Несоответствие");

                return answer;
            }
        }

        // cmd =  0x1300 - для канала 1, 0x2300 - для канала 2, 0x3300 - для канала 3, 0x4300 - для канала 4
        // channel - канал
        private dynamic GetTotalPower(byte cmd, int channel, DateTime dateNow)
        {
            dynamic answer = new ExpandoObject();
            dynamic dt = Send(MakePackage(cmd, 0x00, 0x00, 0x02), 0x4F);
            if (!dt.success)
            {
                log($"Не удалось прочитать: Суммарная активная мощность для канала {channel}");
                return dt;
            }
            else
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                if (dt.Body[2] != 0xff && dt.Body[3] != 0xff)
                {
                    byte[] bytes = new byte[] { dt.Body[3], dt.Body[2], dt.Body[1], dt.Body[0] };
                    double P1 = BitConverter.ToUInt32(bytes, 0) * 0.00512;
                    log($"Суммараня активная мощность для канала {channel}: {P1} Вт");
                    answer.records.Add(MakeDayRecord($"Суммараня активная мощность для канала {channel}", P1, "Вт", dateNow));
                }
                else log($"Суммараня активная мощность для канала {channel}: Несоответствие");

                return answer;
            }
        }
        #endregion

        #region Common
        private enum DeviceError
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
            logger(message, level);
        }

        private byte[] SendSimple(byte[] data, int timeout = 4000, int waitCollectedMax = 2)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);
            
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) >= 0 && !isCollected)
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
                        if (waitCollected == waitCollectedMax)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, byte cmd, int attempts = 3)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempt = 0; (attempt < attempts) && (answer.success == false); attempt++)
            {
                buffer = SendSimple(data);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    //buffer = buffer.SkipWhile(b => b == 0xff).ToArray();
                    var na = GetNaPacket();

                    if (buffer.Length < 6)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 6 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (buffer[0] != data[0])
                    {
                        log("Несовпадение сетевого адреса", level: 1);
                        answer.error = "Несовпадение сетевого адреса";
                        answer.errorcode = DeviceError.ADDRESS_ERROR;
                    }
                    //else if (cmd != data[4])
                    //{
                    //    answer.error = "Несовпадение команды";
                    //    answer.errorcode = DeviceError.ADDRESS_ERROR;
                    //}
                    else
                    {
                        do
                        {
                            if (Crc.Check(buffer, new Crc16Modbus())) break;
                            buffer = buffer.Take(buffer.Length - 1).ToArray();
                        }
                        while (buffer.Length > 6);

                        if (!Crc.Check(buffer, new Crc16Modbus()))
                        {
                            answer.error = "контрольная сумма кадра не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;
                        }
                        else
                        {
                            answer.success = true;
                            answer.error = string.Empty;
                            answer.errorcode = DeviceError.NO_ERROR;
                        }
                    }
                }
            }

            if (answer.success)
            {
                answer.Body = buffer.Take(buffer.Length - 2).Skip(3).ToArray();
            }

            return answer;
        }


        private dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
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

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
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

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
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

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
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

        private dynamic MakeResult(int code, DeviceError errorcode, string description)
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

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion

        #region ImportExport

        [Import("logger")]
        private Action<string, int> logger;

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

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            uint na = 0;
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out na))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            else
            {
                NetworkAddress = na;
            }

            if (param.ContainsKey("settingChannel1"))
            {
                try
                {
                    string[] paramCh1 = arg.settingChannel1.ToString().Split(';');
                    //log($"{paramCh1[0]} --- {paramCh1[1]} --- {paramCh1[2]} --- {paramCh1[3]}");
                    if (paramCh1[0] == "1")
                    {
                        editChannels[0] = true;
                        //log("Запись для канала 1 разрешена");
                        CoeffCh1[0] = Int32.Parse(paramCh1[1].Split('-')[0]);
                        PhaseCh1[0] = Int32.Parse(paramCh1[1].Split('-')[1]);
                        CoeffCh1[1] = Int32.Parse(paramCh1[2].Split('-')[0]);
                        PhaseCh1[1] = Int32.Parse(paramCh1[2].Split('-')[1]);
                        CoeffCh1[2] = Int32.Parse(paramCh1[3].Split('-')[0]);
                        PhaseCh1[2] = Int32.Parse(paramCh1[3].Split('-')[1]);
                        //log($"Получены параметры для канала 1: {paramCh1[1]}, {paramCh1[2]}, {paramCh1[3]}");
                    }
                }
                catch { log("Нет параметров для канала 1 или не верный формат"); editChannels[0] = false; }
            }

            if (param.ContainsKey("settingChannel2"))
            {
                try
                {
                    string[] paramCh2 = arg.settingChannel2.ToString().Split(';');
                    if (paramCh2[0] == "1")
                    {
                        editChannels[1] = true;
                        //log("Запись для канала 2 разрешена");
                        CoeffCh2[0] = Int32.Parse(paramCh2[1].Split('-')[0]);
                        PhaseCh2[0] = Int32.Parse(paramCh2[1].Split('-')[1]);
                        CoeffCh2[1] = Int32.Parse(paramCh2[2].Split('-')[0]);
                        PhaseCh2[1] = Int32.Parse(paramCh2[2].Split('-')[1]);
                        CoeffCh2[2] = Int32.Parse(paramCh2[3].Split('-')[0]);
                        PhaseCh2[2] = Int32.Parse(paramCh2[3].Split('-')[1]);
                        //log($"Получены параметры для канала 2: {paramCh2[1]}, {paramCh2[2]}, {paramCh2[3]}");
                    }
                }
                catch { log("Нет параметров для канала 2 или не верный формат"); editChannels[1] = false; }
            }

            if (param.ContainsKey("settingChannel3"))
            {
                try
                {
                    string[] paramCh3 = arg.settingChannel3.ToString().Split(';');
                    if (paramCh3[0] == "1")
                    {
                        editChannels[2] = true;
                        //log("Запись для канала 3 разрешена");
                        CoeffCh3[0] = UInt16.Parse(paramCh3[1].Split('-')[0]);
                        PhaseCh3[0] = UInt16.Parse(paramCh3[1].Split('-')[1]);
                        CoeffCh3[1] = UInt16.Parse(paramCh3[2].Split('-')[0]);
                        PhaseCh3[1] = UInt16.Parse(paramCh3[2].Split('-')[1]);
                        CoeffCh3[2] = UInt16.Parse(paramCh3[3].Split('-')[0]);
                        PhaseCh3[2] = UInt16.Parse(paramCh3[3].Split('-')[1]);
                        //log($"Получены параметры для канала 3: {paramCh3[1]}, {paramCh3[2]}, {paramCh3[3]}");
                    }
                }
                catch { log("Нет параметров для канала 3 или не верный формат"); editChannels[2] = false; }
            }

            if (param.ContainsKey("settingChannel1"))
            {
                try
                {
                    string[] paramCh4 = arg.settingChannel4.ToString().Split(';');
                    if (paramCh4[0] == "1")
                    {
                        editChannels[3] = true;
                        //log("Запись для канала 4 разрешена");
                        CoeffCh4[0] = UInt16.Parse(paramCh4[1].Split('-')[0]);
                        PhaseCh4[0] = UInt16.Parse(paramCh4[1].Split('-')[1]);
                        CoeffCh4[1] = UInt16.Parse(paramCh4[2].Split('-')[0]);
                        PhaseCh4[1] = UInt16.Parse(paramCh4[2].Split('-')[1]);
                        CoeffCh4[2] = UInt16.Parse(paramCh4[3].Split('-')[0]);
                        PhaseCh4[2] = UInt16.Parse(paramCh4[3].Split('-')[1]);
                        //log($"Получены параметры для канала 4: {paramCh4[1]}, {paramCh4[2]}, {paramCh4[3]}");
                    }
                }
                catch { log("Нет параметров для канала 4 или не верный формат"); editChannels[3] = false; }
            }

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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = All(components);
                        }
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        #endregion

        #region Интерфейс

        private dynamic All(string components)
        {

            ////var time = ReadCurrentTime();
            //if (!time.success)
            //{
            //    log(string.Format("Ошибка при считывании времени на вычислителе: {0}", time.error), level: 1);
            //    return MakeResult(102, time.errorcode, time.error);
            //}

            //var date = time.date;
            //setTimeDifference(DateTime.Now - date);
            //log(string.Format("текущая дата на приборе {0:dd.MM.yyyy HH:mm:ss}", date));
            DateTime date = DateTime.Now;
            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            if (components.Contains("Day"))
            {
                log("Показания токов, напряжения и мощности в отделе ТЕКУЩИЕ");

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                List<dynamic> parameters = new List<dynamic>();

                //var current = GetMaxVoltage(date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetFrequency(date);
                //if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetMaxToque(0x18, 1, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetMaxToque(0x28, 2, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetMaxToque(0x38, 3, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetMaxToque(0x48, 4, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalPower(0x13, 1, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalPower(0x23, 2, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalPower(0x33, 3, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalPower(0x43, 4, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //records(parameters);
                //List<dynamic> currents = parameters;
                //log(string.Format("Текущие на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
            }

            if (components.Contains("Current"))
            {
                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                List<dynamic> parameters = new List<dynamic>();

                var current = GetVoltage(date);
                if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetAmper(0x14, 1, date);
                Ch1TrmsArr[0] = current.toque1;
                Ch1TrmsArr[1] = current.toque2;
                Ch1TrmsArr[2] = current.toque3;
                if (current. success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetAmper(0x24, 2, date);
                Ch2TrmsArr[0] = current.toque1;
                Ch2TrmsArr[1] = current.toque2;
                Ch2TrmsArr[2] = current.toque3;
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetAmper(0x34, 3, date);
                Ch3TrmsArr[0] = current.toque1;
                Ch3TrmsArr[1] = current.toque2;
                Ch3TrmsArr[2] = current.toque3;
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetAmper(0x44, 4, date);
                Ch4TrmsArr[0] = current.toque1;
                Ch4TrmsArr[1] = current.toque2;
                Ch4TrmsArr[2] = current.toque3;
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                KoefPower1 = GetPowerCoeff(0x10, 1);
                KoefPower2 = GetPowerCoeff(0x20, 2);
                KoefPower3 = GetPowerCoeff(0x30, 3);
                KoefPower4 = GetPowerCoeff(0x40, 4);

                current = GetPower(0x13, 1, date, Ch1TrmsArr);
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetPower(0x23, 2, date, Ch2TrmsArr);
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetPower(0x33, 3, date, Ch3TrmsArr);
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                current = GetPower(0x43, 4, date, Ch4TrmsArr);
                if (current.success) parameters.AddRange(current.records);

                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetActivePower(0x12, 1, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetActivePower(0x22, 2, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetActivePower(0x32, 3, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetActivePower(0x42, 4, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (!current.success)
                //{
                //    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                //    return MakeResult(102, current.errorcode, current.error);
                //}

                records(parameters);
                List<dynamic> currents = parameters;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
            }

            ////

            if (components.Contains("Constant"))
            {
                GetCoeff(0x14, 0x60, 1, "L1");
                GetPhase(0x14, 0x63, 1, "L1");

                GetCoeff(0x14, 0x61, 1, "L2");
                GetPhase(0x14, 0x64, 1, "L2");

                GetCoeff(0x14, 0x62, 1, "L3");
                GetPhase(0x14, 0x65, 1, "L3");
                ////
                GetCoeff(0x24, 0x60, 2, "L1");
                GetPhase(0x24, 0x63, 2, "L1");

                GetCoeff(0x24, 0x61, 2, "L2");
                GetPhase(0x24, 0x64, 2, "L2");

                GetCoeff(0x24, 0x62, 2, "L3");
                GetPhase(0x24, 0x65, 2, "L3");
                ///
                GetCoeff(0x34, 0x60, 3, "L1");
                GetPhase(0x34, 0x63, 3, "L1");

                GetCoeff(0x34, 0x61, 3, "L2");
                GetPhase(0x34, 0x64, 3, "L2");

                GetCoeff(0x34, 0x62, 3, "L3");
                GetPhase(0x34, 0x65, 3, "L3");
                ///
                GetCoeff(0x44, 0x60, 4, "L1");
                GetPhase(0x44, 0x63, 4, "L1");

                GetCoeff(0x44, 0x61, 4, "L2");
                GetPhase(0x44, 0x64, 4, "L2");

                GetCoeff(0x44, 0x62, 4, "L3");
                GetPhase(0x44, 0x65, 4, "L3");

                if (editChannels[0]) 
                {
                    SetPhase(0x14, 0x63, PhaseCh1[0], 1, "L1");
                    SetPhase(0x14, 0x64, PhaseCh1[1], 1, "L2");
                    SetPhase(0x14, 0x65, PhaseCh1[2], 1, "L3");
                    SetCoeff(0x14, 0x60, CoeffCh1[0], 1, "L1");
                    SetCoeff(0x14, 0x61, CoeffCh1[1], 1, "L2");
                    SetCoeff(0x14, 0x62, CoeffCh1[2], 1, "L3");
                }
                if (editChannels[1])
                {
                    SetCoeff(0x24, 0x60, CoeffCh2[0], 1, "L1");
                    SetCoeff(0x24, 0x61, CoeffCh2[1], 1, "L2");
                    SetCoeff(0x24, 0x62, CoeffCh2[2], 1, "L3");
                    SetPhase(0x24, 0x63, PhaseCh2[0], 2, "L1");
                    SetPhase(0x24, 0x64, PhaseCh2[1], 2, "L2");
                    SetPhase(0x24, 0x65, PhaseCh2[2], 2, "L3");
                }
                if (editChannels[2])
                {
                    SetCoeff(0x34, 0x60, CoeffCh3[0], 1, "L1");
                    SetCoeff(0x34, 0x61, CoeffCh3[1], 1, "L2");
                    SetCoeff(0x34, 0x62, CoeffCh3[2], 1, "L3");
                    SetPhase(0x34, 0x63, PhaseCh3[0], 3, "L1");
                    SetPhase(0x34, 0x64, PhaseCh3[1], 3, "L2");
                    SetPhase(0x34, 0x65, PhaseCh3[2], 3, "L3");
                }
                if (editChannels[3])
                {
                    SetCoeff(0x44, 0x60, CoeffCh4[0], 1, "L1");
                    SetCoeff(0x44, 0x61, CoeffCh4[1], 1, "L2");
                    SetCoeff(0x44, 0x62, CoeffCh4[2], 1, "L3");
                    SetPhase(0x44, 0x63, PhaseCh4[0], 3, "L1");
                    SetPhase(0x44, 0x64, PhaseCh4[1], 3, "L2");
                    SetPhase(0x44, 0x65, PhaseCh4[2], 3, "L3");
                }


                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //List<dynamic> parameters = new List<dynamic>();

                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalEnergy(0x12, 1, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalEnergy(0x22, 2, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalEnergy(0x32, 3, date);
                //if (current.success) parameters.AddRange(current.records);

                //if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

                //current = GetTotalEnergy(0x42, 4, date);
                //if (current.success) parameters.AddRange(current.records);

                ////if (!current.success)
                ////{
                ////    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                ////    return MakeResult(102, current.errorcode, current.error);
                ////}

                //records(parameters);
                //List<dynamic> currents = parameters;
                //log(string.Format("Константы на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
            }

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        #endregion

        #region Convert
        private static int ConvertFromBcd(byte bcd)
        {
            return ConvertFromBcd(new byte[] { bcd }, 0, 1);
        }
        private static int ConvertFromBcd(byte[] bcd, int startIndex, int length)
        {
            if (bcd == null || startIndex < 0 || length <= 0 || startIndex >= bcd.Length || startIndex + length > bcd.Length)
                return 0;

            string str = string.Empty;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                str += bcd[i].ToString("X");
            }
            int result = 0;
            if (int.TryParse(str, out result))
            {
                return result;
            }
            return 0;
        }
        private byte ConvertToBcd(int value)
        {
            var valStr = value.ToString();
            return Convert.ToByte(valStr, 16);
            //byte result;
            //byte.TryParse("0x" + valStr, out result);
            //return result;
        }
        #endregion
    }
}
