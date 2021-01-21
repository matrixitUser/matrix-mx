using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    internal class FlashConfig
    {
        private const byte COUNTERS = 4;

        public UInt16 u16FlashVer;
        public UInt16 u16CounterFilter;
        public UInt16 u16CounterAlarmTh;
        public UInt16 u16CounterAlarmMax;

        public UInt32 u32BaudRate;

        public USART_WordLength eWordLen;
        public USART_StopBits eStopBits;
        public USART_Parity eParity;

        public byte u8NetworkAddress;

        public UInt16[] au16Parameter;
        public UInt16[] au16Unit;
        public float[] afMul;
        public float[] afOffset;

        public FlashConfig(byte[] config)
        {
            au16Parameter = new ushort[COUNTERS];
            au16Unit = new ushort[COUNTERS];
            afMul = new float[COUNTERS];
            afOffset = new float[COUNTERS];

            u16FlashVer = BitConverter.ToUInt16(config, 0);

            if (u16FlashVer >= 0xAA64)
            {
                u8NetworkAddress = config[2];
                //config[3] - reserved

                //sUart - 4
                u32BaudRate = BitConverter.ToUInt32(config, 4);

                eWordLen = (USART_WordLength)config[8];
                eStopBits = (USART_StopBits)config[9];
                eParity = (USART_Parity)config[10];
                //config[11] - reserved

                //asCounter - 12
                for (var i = 0; i < COUNTERS; i++)
                {
                    au16Parameter[i] = BitConverter.ToUInt16(config, 12 + i * 12);
                    au16Unit[i] = BitConverter.ToUInt16(config, 12 + i * 12 + 2);
                    afMul[i] = BitConverter.ToSingle(config, 12 + i * 12 + 4);
                    afOffset[i] = BitConverter.ToSingle(config, 12 + i * 12 + 8);
                }

                //
                var offset = 12 + COUNTERS * 12;
                u16CounterFilter = BitConverter.ToUInt16(config, offset + 0);
                u16CounterAlarmTh = BitConverter.ToUInt16(config, offset + 2);
                u16CounterAlarmMax = BitConverter.ToUInt16(config, offset + 4);
            }
            else if (u16FlashVer >= 0xAA00)
            {
                u16CounterFilter = BitConverter.ToUInt16(config, 2);
                u16CounterAlarmTh = BitConverter.ToUInt16(config, 4);
                u16CounterAlarmMax = BitConverter.ToUInt16(config, 6);

                u32BaudRate = BitConverter.ToUInt32(config, 8);

                eWordLen = (USART_WordLength)config[12];
                eStopBits = (USART_StopBits)config[13];
                eParity = (USART_Parity)config[14];

                u8NetworkAddress = config[15];

                for (var i = 0; i < COUNTERS; i++)
                {
                    au16Parameter[i] = BitConverter.ToUInt16(config, 16 + i * 2);
                    au16Unit[i] = BitConverter.ToUInt16(config, 24 + i * 2);
                    afMul[i] = BitConverter.ToSingle(config, 32 + i * 4);
                    afOffset[i] = BitConverter.ToSingle(config, 48 + i * 4);
                }
            }
            else if(u16FlashVer >= 0x0100)
            {           
                //!!! VERSION = 01XXh / 02XXh !!!
                //typedef struct
                //{
                //    u16 u16FlashVer;                  0
                //    u8 u8NetworkAddress;              2
                //    u8 reserved;

                //    u32 u32SupplyStartInterval;       4

                //    //uart
                //    tsUart sUart;                     8                     

                //    //supplymgr
                //    tsSupplyMgr sSupplyMgr;           16

                //    u8 channels;                      -/26
                //    u8 counters;                      -/27

                //    //channel
                //    tsChannel asChannel[CHANNELS];    26/28

                //

                //    //counter
                //    tsCounter asCounter[COUNTERS];    90/92
                //} tsConfig;
                
                //typedef struct
                //{
                //    u32 u32BaudRate;
                //    u8 u8WordLen;
                //    u8 u8StopBits;
                //    u8 u8Parity;
                //    u8 reserved;
                //} tsUart;

                //typedef struct
                //{
                //    u16 u16InitLowTime;
                //    u16 u16InitHighTime;
                //    u16 u16ChargePeriod;
                //    u16 u16PreDischargePeriod;
                //    u16 u16DischargeTime;
                //} tsSupplyMgr;

                //typedef struct
                //{
                //    u8 en;
                //    u8 portsrc;
                //    u8 mode;
                //    u8 trig;
                //} tsChannel;


                //typedef struct
                //{
                //    u16 u16Parameter;
                //    u16 u16Unit;
                //    float fMul;
                //    float fOffset;
                //} tsCounter;

                var uartOffset = 8;

                var channelsOffset = 26;
                var channelsTotal = 16;

                var countersOffset = 90;
                var countersTotal = 4;

                if (u16FlashVer == 0x5180)
                {
                    uartOffset = 4;
                    channelsTotal = config[12];
                    countersTotal = config[13];
                    channelsOffset = 14;
                    countersOffset = 14 + 64;
                }
                else if(u16FlashVer >= 0x0200)
                {
                    channelsOffset = 28;
                    countersOffset = 92;
                    channelsTotal = config[26];
                    countersTotal = config[27];
                }

                u8NetworkAddress = config[2];

                //sUart - 8
                u32BaudRate = BitConverter.ToUInt32(config, uartOffset + 0);
                eWordLen = (USART_WordLength)config[uartOffset + 4];
                eStopBits = (USART_StopBits)config[uartOffset + 5];
                eParity = (USART_Parity)config[uartOffset + 6];
                //config[11] - reserved

                //asCounter - 90/92
                for (var i = 0; i < countersTotal; i++)
                {
                    au16Parameter[i] =  BitConverter.ToUInt16(config, countersOffset + i * 12 + 0);
                    au16Unit[i] =       BitConverter.ToUInt16(config, countersOffset + i * 12 + 2);
                    afMul[i] =          BitConverter.ToSingle(config, countersOffset + i * 12 + 4);
                    afOffset[i] =       BitConverter.ToSingle(config, countersOffset + i * 12 + 8);
                }

                //
                u16CounterFilter = 2;
                u16CounterAlarmTh = 0;
                u16CounterAlarmMax = 0xFF;
            }

        }

        public IEnumerable<byte> GetConfig()
        {
            var config = new List<byte>();

            config.AddRange(BitConverter.GetBytes(u16FlashVer));
            config.AddRange(BitConverter.GetBytes(u16CounterFilter));
            config.AddRange(BitConverter.GetBytes(u16CounterAlarmTh));
            config.AddRange(BitConverter.GetBytes(u16CounterAlarmMax));

            config.AddRange(BitConverter.GetBytes(u32BaudRate));

            config.Add((byte)eWordLen);
            config.Add((byte)eStopBits);
            config.Add((byte)eParity);

            config.Add(u8NetworkAddress);

            for (var i = 0; i < COUNTERS; i++)
            {
                config.AddRange(BitConverter.GetBytes(au16Parameter[i]));
            }
            for (var i = 0; i < COUNTERS; i++)
            {
                config.AddRange(BitConverter.GetBytes(au16Unit[i]));
            }
            for (var i = 0; i < COUNTERS; i++)
            {
                config.AddRange(BitConverter.GetBytes(afMul[i]));
            }
            for (var i = 0; i < COUNTERS; i++)
            {
                config.AddRange(BitConverter.GetBytes(afOffset[i]));
            }

            return config;
        }

        //public IEnumerable<Constant> GetConstants()
        //{
        //    var constants = new List<dynamic>();
        //    constants.Add(new Constant("NetworkAddress", string.Format("{0}", u8NetworkAddress)));
        //    constants.Add(new Constant("USART Baudrate", string.Format("{0}", u32BaudRate)));
        //    constants.Add(new Constant("USART Wordlength", eWordLen == USART_WordLength.USART_WordLength_8b ? "8" : "9"));
        //    constants.Add(new Constant("USART Parity", eParity == USART_Parity.USART_Parity_No ? "N" : eParity == USART_Parity.USART_Parity_Even ? "E" : "O"));
        //    constants.Add(new Constant("USART StopBits", (eStopBits == USART_StopBits.USART_StopBits_0_5 ? "0.5" : eStopBits == USART_StopBits.USART_StopBits_1 ? "1" :
        //            eStopBits == USART_StopBits.USART_StopBits_1_5 ? "1.5" : "2")));

        //    for (var i = 0; i < 4; i++)
        //    {
        //        constants.Add(new Constant(string.Format("Multiplier{0}", i + 1), string.Format("{0}", afMul[i])));
        //        constants.Add(new Constant(string.Format("Offset{0}", i + 1), string.Format("{0}", afOffset[i])));
        //    }
        //    /*
        //        string.Join(";", au16Parameter),
        //        string.Join(";", au16Unit),
        //        string.Join(";", afMul),
        //        string.Join(";", afOffset)*/
        //    return constants;

        //}

        public override string ToString()
        {
            return string.Format("NA:{0} USART:{1} {2}{3}{4}",// Параметры:<{5}> Ед.изм:<{6}> Множ:<{7}> Смещ:<{8}>",
                u8NetworkAddress,
                u32BaudRate,
                eWordLen == USART_WordLength.USART_WordLength_8b ? "8" : "9",
                eParity == USART_Parity.USART_Parity_No ? "N" : eParity == USART_Parity.USART_Parity_Even ? "E" : "O",
                (eStopBits == USART_StopBits.USART_StopBits_0_5 ? "0.5" : eStopBits == USART_StopBits.USART_StopBits_1 ? "1" :
                    eStopBits == USART_StopBits.USART_StopBits_1_5 ? "1.5" : "2"),
                string.Join(";", au16Parameter),
                string.Join(";", au16Unit),
                string.Join(";", afMul),
                string.Join(";", afOffset)
                );
        }

        public enum USART_WordLength
        {
            USART_WordLength_8b = 0x00,
            USART_WordLength_9b = 0x10
        }

        public enum USART_StopBits
        {
            USART_StopBits_1 = 0x00,
            USART_StopBits_0_5 = 0x10,
            USART_StopBits_2 = 0x20,
            USART_StopBits_1_5 = 0x30,
        }

        public enum USART_Parity
        {
            USART_Parity_No = 0x00,
            USART_Parity_Even = 0x04,
            USART_Parity_Odd = 0x06,
        }
    }
}
