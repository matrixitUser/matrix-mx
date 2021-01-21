using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public static class Helper
    {
        private static Dictionary<int, double[]> QmaxDict = new Dictionary<int, double[]>()
        {
            { 10, new double[] { 0.25, 0.32, 0.40, 0.50, 0.60, 0.80, 1.00, 1.25, 1.60, 2.00, 2.50 } },
            { 15, new double[] { 0.60, 0.80, 1.00, 1.25, 1.60, 2.00, 2.50, 3.20, 4.00, 5.00, 6.00 } },
            { 25, new double[] { 1.60, 2.00, 2.50, 3.20, 4.00, 5.00, 6.00, 8.00, 10.00, 12.50, 16.00 } },
            { 40, new double[] { 4.00, 5.00, 6.00, 8.00, 10.00, 12.50, 16.00, 20.00, 25.00, 32.00, 40.00 } },
            { 50, new double[] { 6.00, 8.00, 10.00, 12.50, 16.00, 20.00, 25.00, 32.00, 40.00, 50.00, 60.00 } },
            { 80, new double[] { 16.00, 20.00, 25.00, 32.00, 40.00, 50.00, 60.00, 80.00, 100.00, 125.00, 160.00 } },
            { 100, new double[] { 25.00, 32.00, 40.00, 50.00, 60.00, 80.00, 100.00, 125.00, 160.00, 200.00, 250.00 } },
            { 150, new double[] { 60.00, 80.00, 100.00, 125.00, 160.00, 200.00, 250.00, 320.00, 400.00, 500.00, 600.00 } },
            { 200, new double[] { 100.00, 125.00, 160.00, 200.00, 250.00, 320.00, 400.00, 500.00, 600.00, 800.00, 1000.00 } },
            { 300, new double[] { 250.00, 320.00, 400.00, 500.00, 600.00, 800.00, 1000.00, 1250.00, 1600.00, 2000.00, 2500.00 } },
            { 400, new double[] { 400.00, 500.00, 600.00, 800.00, 1000.00, 1250.00, 1600.00, 2000.00, 2500.00, 3200.00, 4000.00 } }
        };

        private static int[] DummArray = new int[] { 10, 15, 25, 40, 50, 80, 100, 150, 200, 300, 400 };
        private static int[] QminArray = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private static int[] numOfCurArray = new int[] { 0, 1, 2 };
        private static string[] rangeIoutArray = new string[] { "0-5", "0-20", "4-20" };
        private static string[] rangeIinArray = new string[] { "0-5", "0-20", "4-20" };
        private static string[] paramIoutArray1 = new string[] { "Q1", "T1", "T2", "dT", "p1", "p2" };
        private static string[] paramIoutArray2 = new string[] { "Q1", "Q2", "T1", "T2", "dT", "p1", "p2" };
        private static string[] termosensorTypeArray = new string[] { "100P", "Pt100", "100M" };
        private static double[] pmaxArray = new double[] { 0.40, 0.60, 1.00, 1.60, 2.50, 4.00 };

        public static UInt16 ToUInt16Reverse(byte[] bytes, int offset)
        {
            return BitConverter.ToUInt16(bytes.Skip(offset).Take(2).Reverse().ToArray(), 0);
        }

        public static Int16 ToInt16Reverse(byte[] bytes, int offset)
        {
            return BitConverter.ToInt16(bytes.Skip(offset).Take(2).Reverse().ToArray(), 0);
        }

        public static UInt32 ToUInt32Reverse(byte[] bytes, int offset)
        {
            return BitConverter.ToUInt32(bytes.Skip(offset).Take(4).Reverse().ToArray(), 0);
        }

        public static Int32 ToInt32Reverse(byte[] bytes, int offset)
        {
            return BitConverter.ToInt32(bytes.Skip(offset).Take(4).Reverse().ToArray(), 0);
        }

        public static int GetDummFromByte(byte n, Version ver)
        {
            return (n < DummArray.Length)? DummArray[n] : 0;
        }

        public static double GetQmaxByDumm(byte n, int Dumm)
        {
            double ret = 0.0;
            if(QmaxDict.ContainsKey(Dumm) && (n < QmaxDict[Dumm].Length))
            {
                ret = QmaxDict[Dumm][n];
            }
            return ret;
        }

        public static int GetQminFromByte(byte n)
        {
            return (n < QminArray.Length) ? QminArray[n] : 0;
        }

        public static int GetNumOfCurrentsFromByte(byte n)
        {
            return (n < numOfCurArray.Length) ? numOfCurArray[n] : 0;
        }

        public static string GetRangeIoutFromByte(byte n)
        {
            return (n < rangeIoutArray.Length) ? rangeIoutArray[n] : "";
        }

        public static string GetRangeIinFromByte(byte n)
        {
            return (n < rangeIinArray.Length) ? rangeIinArray[n] : "";
        }

        public static string GetParamIoutFromByte(byte n, VersionHardware verHw)
        {
            if(verHw == VersionHardware.SA94_1)
            {
                return (n < paramIoutArray1.Length) ? paramIoutArray1[n] : "";
            }
            return (n < paramIoutArray2.Length) ? paramIoutArray2[n] : "";
        }

        public static string GetTermosensorTypeFromByte(byte n)
        {
            return (n < termosensorTypeArray.Length) ? termosensorTypeArray[n] : "";
        }

        public static double GetpmaxFromByte(byte n)
        {
            return (n < pmaxArray.Length) ? pmaxArray[n] : 0;
        }
    }
}
