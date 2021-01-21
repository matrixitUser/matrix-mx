using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetPassport(byte na, string version)
        {
            var passportLength = 978;
            if (version.Contains("Z") || version.Contains("X"))
            {
                passportLength = 722;
            }

            var passport = ParsePassport(GetBlocks(na, 0x98, 1, 1, passportLength), version);
            return passport;
        }

        private dynamic ParsePassport(byte[] bytes, string version)
        {
            dynamic passport = new ExpandoObject();
            passport.constants = new List<dynamic>();

            if (bytes == null || !bytes.Any())
            {
                passport.success = false;
                passport.error = "нет данных для разбора";
                return passport;
            }

            var channelCount = 24;
            if (version.Contains("Z") || version.Contains("X"))
            {
                channelCount = 16;
            }

            passport.channels = new List<dynamic>();

            const int channelLength = 32;

            for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                var channel = ParseChannel(bytes.Skip(channelIndex * channelLength).Take(channelLength).ToArray());
                if (!channel.success)
                {
                    passport.success = false;
                    passport.error = channel.error;
                    return passport;
                }
                passport.constants.Add(MakeConstRecord(string.Format("верх. предел {0}{1} {2}", channel.name, channel.number, channel.unit), channel.max, DateTime.Now));
                passport.constants.Add(MakeConstRecord(string.Format("ниж. предел {0}{1} {2}", channel.name, channel.number, channel.unit), channel.min, DateTime.Now));
                passport.channels.Add(channel);
            }

            var constOffset = 951;
            if (version.Contains("Z") || version.Contains("X"))
            {
                constOffset = 695;
            }

            passport.lBlock = BitConverter.ToUInt16(new byte[] { bytes[constOffset + 8], bytes[constOffset + 9] }, 0);
            passport.lRec = bytes[constOffset + 11];

            passport.task = bytes[constOffset + 17];

            passport.success = true;
            return passport;
        }

        private dynamic ParseChannel(byte[] bytes)
        {
            dynamic channel = new ExpandoObject();
            channel.success = true;

            if (bytes == null || bytes.Length < 32)
            {
                channel.success = false;
                channel.error = "недостаточно данных для разбора канала";
                return channel;
            }

            channel.number = bytes[1] & 0x07;

            var pcode = (byte)(bytes[1] & 0xf8);
            channel.name = GetChannelName(pcode);

            channel.notUsed = bytes[0] == 0x00;

            //ChannelSign = (ChannelSign)(data[0] & 0x80);
            //PhysicChannelNumber = (int)(data[0] & 0x1f);
            //ParameterName = (ParameterName)(data[1] & 0xf8);


            channel.type = (ChannelType)(bytes[2] & 0xf0);
            channel.isSummed = channel.type == ChannelType.S ||
                channel.type == ChannelType.P ||
                channel.type == ChannelType.T;

            //Factor = BitConverter.ToSingle(data, 19) / 2f;
            //IsRegistrar = ((data[18] & 0x01) == 1);


            channel.koefficient = BitConverter.ToSingle(bytes, 19) / 2f;

            channel.isRegister = bytes[18] == 1;

            channel.unit = GetUnit(channel.name, (byte)(bytes[17] & 0xff));

            //Group = (data[16] & 0xf0) >> 4;
            channel.isOff = new Func<bool>(() => channel.notUsed || !channel.isRegister);

            channel.max = BitConverter.ToSingle(bytes, 4) / 2f;
            channel.min = BitConverter.ToSingle(bytes, 8) / 2f;

            channel.floor = GetFloor(bytes[29]);

            return channel;
        }

        private string GetChannelName(byte code)
        {
            switch (code)
            {
                case 0x08: return Glossary.T;
                case 0x10: return Glossary.P;
                case 0xA8: return Glossary.Qd;
                case 0xB0: return Glossary.Ge;
                case 0x18: return Glossary.dP;
                case 0x20: return Glossary.H;
                case 0x28: return Glossary.Qo;
                case 0xB8: return Glossary.Gr;
                case 0x30: return Glossary.Go;
                case 0x31: return Glossary.dGo;
                case 0xC0: return Glossary.Ro;
                case 0x38: return Glossary.Qm;
                case 0x40: return Glossary.Gm;
                case 0xC8: return Glossary.Me;
                case 0x48: return Glossary.Qn;
                case 0x50: return Glossary.Gn;
                case 0xD0: return Glossary.N;
                case 0x68: return Glossary.tm;
                case 0x70: return Glossary.Pa;
                case 0x78: return Glossary.ts;
                case 0xD8: return Glossary.I;
                case 0x80: return Glossary.Sw;
                case 0x88: return Glossary.Pb;
                case 0xE0: return Glossary.F;
                case 0x90: return Glossary.L;
                case 0x98: return Glossary.Nnn;
                case 0xE8: return Glossary.dT;
                case 0xA0: return Glossary.Gf;
                case 0xF0: return Glossary.Qw;
                case 0xF8: return Glossary.Gw;
                case 0xb9: return Glossary.Ron;
                default: return "параметр код " + code.ToString("X2");
            }
        }

        private string GetUnit(string parameter, byte code)
        {
            switch (parameter)
            {
                //температура, разность температур
                case Glossary.T:
                case Glossary.dT:
                    switch (code)
                    {
                        case 0x01: return "°C";
                    }
                    break;
                //давление, перепад
                case Glossary.P:
                case Glossary.dP:
                case Glossary.Pa:
                case Glossary.Pb:
                    switch (code)
                    {
                        case 0x13: return "МПа";
                        case 0x11: return "кгс/см²";
                        case 0x12: return "кгс/м²";
                        case 0x14: return "кПа";
                        case 0x15: return "мм рт.ст.";
                    }
                    break;
                //расход объемный, расход рабочий
                case Glossary.Qo:
                case Glossary.Qw:
                case Glossary.Qd:
                    switch (code)
                    {
                        case 0x91: return "м³∙ч";
                        case 0x92: return "тыс. м³/ч";
                    }
                    break;
                //расход норм.об.
                case Glossary.Qn:
                    switch (code)
                    {
                        case 0xA1: return "м³/ч";
                        case 0xA2: return "тыс. м³/ч";
                    }
                    break;
                case Glossary.Gn:
                case Glossary.Gr:
                    switch (code)
                    {
                        case 0xB1: return "м³";
                        case 0xB2: return "тыс. м³";
                    }
                    break;
                case Glossary.Go:
                    switch (code)
                    {
                        case 0x31: return "м³";
                        case 0x32: return "тыс. м³";
                    }
                    break;
                ///наработка
                case Glossary.tm:
                case Glossary.ts:
                    switch (code)
                    {
                        case 0x51: return "ч";
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

        private int GetFloor(byte code)
        {
            switch (code)
            {
                case 0x60: return 0;
                case 0x61: return 1;
                case 0x62: return 2;
                case 0x63: return 3;
                case 0x64: return 4;
                case 0x65: return 5;
                default: return 5;
            }
        }
    }
}
