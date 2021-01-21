using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host
{
    public class StructsHelper
    {
        private static readonly StructsHelper instance = new StructsHelper();
        public static StructsHelper Instance
        {
            get
            {
                return instance;
            }
        }
        public byte[] Parse24BytesFromString(string strTmp)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes(strTmp));
            while (bytes.Count < 24) bytes.Add(0);
            return bytes.ToArray();
        }
        public string ParseStringFromBytes(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes).Replace("\0", "").Replace(" ", "");
        }

        public T setBytesFromConfig<T>(byte[] arr, T type)
        {
            int size = Marshal.SizeOf(type);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            type = (T)Marshal.PtrToStructure(ptr, type.GetType());
            Marshal.FreeHGlobal(ptr);

            return type;
        }
        public byte[] getBytes<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }

    public struct UartConfig
    {
        public UInt32 u32BaudRate;
        public byte u8WordLen;
        public byte u8StopBits;
        public byte u8Parity;
        public byte reserved;
    }
    
    #region matrix terminal
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct Profiles
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] ip_port;    //24
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct tsApnName
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] APN; // 24
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct MatrixTerminalConfig  //200 байт
    {
        public UInt16 u16FlashVer;          //2 
        public byte u8NetworkAddress;       //3
        public byte u8Mode;                 //4

        public UInt32 u32ReleaseTs;         //8
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Profiles[] profile;   //3  //3*48 = 144 байта 8+144=152

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public tsApnName[] apnName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public UartConfig[] sUart;         //160
        //public UartConfig sUart2;
        //public UartConfig sUart3;     //по 8 байт * 3 = 24 байта 24+152=176

        public UInt16 PeriodEvent;      // Период опроса нештатных ситуации //178
        public byte apnCount;         // Использование профилей побитно: Нулевой бит- нулевой профиль //179
        public byte profileCount;       // Количество профилей   = 4 байта      //180


        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public UInt32[] u32CounterNA;     //Сетевые адреса счетчиков  	= 16 байтов   //196

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] u8CounterType;      //Типы счетчиков   = 4 байта              //200

    }
    #endregion
    #region lightV2 structure
    /// <summary>
    /// структура контроллера освещения v2
    /// </summary>

    public struct LigthtChannel
    {
        public byte u8ControlMode;
        public byte u8beforeSunRise;
        public byte u8afterSunSet;
        public byte reserved;
        public UInt32 on1;
        public UInt32 off1;
        public UInt32 on2;
        public UInt32 off2;
    }

    public struct LightConfig
    {
        public UInt16 u16FlashVer;
        public byte u8NetworkAddress;
        public byte u8Mode;

        public UartConfig sUart1;
        public UartConfig sUart2;
        public UartConfig sUart3;

        public UInt32 u32ReleaseTs;

        public UInt16 u16TimeOut;
        public byte u8IsRtcError;
        public byte u8timeDiff;//+

        public UInt32 u32lat;//+
        public UInt32 u32lon; //+

        public LigthtChannel ligthtChannels1;//+
        public LigthtChannel ligthtChannels2;//+
        public LigthtChannel ligthtChannels3;//+
        public LigthtChannel ligthtChannels4;//+

        public byte u8hardware;
        public byte u8reserved;
        public UInt16 u16reserved;
    }
    #endregion
}
