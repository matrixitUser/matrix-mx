using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetPassportA(byte na)
        {
            return ParsePasportA(GetBlocks(na, 0xC8, 1, 1, 2015));
        }

        private dynamic ParsePasportA(byte[] bytes)
        {
            dynamic passport = new ExpandoObject();
            passport.success = true;

            if (bytes == null || bytes.Length < 2015)
            {
                passport.success = false;
                passport.error = "недостаточно данных";
                return passport;
            }

            const int CHANNEL_SIZE = 64;

            passport.channels = new List<dynamic>();

            //31 строка по 64 байта
            for (var channelStart = 0; channelStart <= 1984 - CHANNEL_SIZE; channelStart += CHANNEL_SIZE)
            {
                var channel = ParseChannelA(bytes.Skip(channelStart).Take(CHANNEL_SIZE).ToArray());
                if (!channel.success)
                {
                    passport.success = false;
                    passport.error = channel.error;
                    return passport;
                }
                passport.channels.Add(channel);
            }

            passport.channelCount = bytes[1988];
            passport.contractHour = bytes[1994];

            string path = @"D:\Im2300\";
            string fileName = string.Format("{0}passport_{1:dd-HH.mm.ss.fff}.txt", path, DateTime.Now);
            //System.IO.File.WriteAllText(fileName, string.Join(" ", bytes.Select(b => b.ToString("X2"))));

            return passport;
        }


        private dynamic ParseChannelA(byte[] bytes)
        {
            dynamic channel = new ExpandoObject();
            channel.success = true;

            if (bytes == null || bytes.Length < 64)
            {
                channel.success = false;
                channel.error = "недостаточно данных для разбора канала";
                return channel;
            }


            channel.name = GetChannelName(bytes[0]);
            channel.number = bytes[1] & 0x1f;

            channel.koefficient = BitConverter.ToSingle(bytes, 23);

            channel.unit = GetUnitA(channel.name, bytes[31]);
            channel.notUsed = bytes[0] == 0x00;

            channel.isOff = new Func<bool>(() => channel.notUsed);

            channel.max = BitConverter.ToSingle(bytes, 22) / 2f;
            channel.min = BitConverter.ToSingle(bytes, 26) / 2f;
            return channel;
        }

        private string GetUnitA(string parameter, byte code)
        {
            switch (parameter)
            {
                //температура, разность температур
                case Glossary.T:
                case Glossary.dT:
                    switch (code)
                    {
                        case 0x00: return "°C";
                    }
                    break;
                //давление, перепад
                case Glossary.P:
                case Glossary.dP:
                case Glossary.Pa:
                case Glossary.Pb:
                    switch (code)
                    {
                        case 0x00: return "МПа";
                        case 0x01: return "кгс/см²";
                        case 0x02: return "кгс/м²";
                        case 0x03: return "кПа";
                        case 0x04: return "мм рт.ст.";
                    }
                    break;
                //расход объемный, расход рабочий
                case Glossary.Qo:
                case Glossary.Qw:
                case Glossary.Qd:
                    switch (code)
                    {
                        case 0x00: return "м³∙ч";
                        case 0x01: return "тыс. м³/ч";
                        case 0x02: return "л/сек";
                        case 0x03: return "м³/мин";
                    }
                    break;
                //расход норм.об.
                case Glossary.Qn:
                    switch (code)
                    {
                        case 0x00: return "м³/ч";
                        case 0x01: return "тыс. м³/ч";
                        case 0x02: return "тыс. м³/мин";
                    }
                    break;
                case Glossary.Gr:
                case Glossary.Gn:
                case Glossary.Go:
                    switch (code)
                    {
                        case 0x00: return "м³";
                        case 0x01: return "тыс. м³";
                        case 0x02: return "л";
                    }
                    break;
                ///наработка
                case Glossary.tm:
                case Glossary.ts:
                    switch (code)
                    {
                        case 0x00: return "ч";
                    }
                    break;
                ///плотность
                case Glossary.Ron:
                case Glossary.Ro:
                    switch (code)
                    {
                        case 0x00: return "кг/м³";
                        case 0x01: return "г/см³";
                    }
                    break;
            }
            return "";
        }
    }
}
