//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    public class ID
//    {
//        private byte contractHour;
//        private DateTime current;
//        private List<Run> runs;

//        public byte ContractHour
//        {
//            get
//            {
//                return contractHour;
//            }
//        }
//        public DateTime Current
//        {
//            get
//            {
//                return current;
//            }
//        }
//        public IEnumerable<Run> Runs
//        {
//            get
//            {
//                return runs;
//            }
//        }

//        private ID() { }

//        public static ID Parse(byte[] data)
//        {
//            ID id = null;

//            if (data != null && data.Length > 1)
//            {
//                byte runsNumber = data[1];

//                if (data.Length == 60)//>= (2 + runsNumber * 17 + 7))
//                {
//                    id = new ID();
//                    id.runs = new List<Run>();

//                    for (int i = 0; i < runsNumber; i++)
//                    {
//                        string runName = Encoding.ASCII.GetString(data, 2 + 17 * i, 16);//.Substring(2 + 17 * i, 16);//new String(tem);
//                        //char[] char_runName = new char[16];
//                        //Array.Copy(data, 2 + 17 * i, char_runName, 0, 16);
//                        //string runName = new string(char_runName);
//                        byte meterType = data[2 + 17 * i + 16];
//                        id.runs.Add(new Run(runName, meterType));
//                    }

//                    const int dateOffset = 53; // 2 + 17 * runsNumber;

//                    var archiveRecordDateTimeString = string.Format("{0}.{1}.{2} {3}:{4}:{5}",
//                        data[dateOffset + 1],//day
//                        data[dateOffset + 0],//mon
//                        data[dateOffset + 2],//yr
//                        data[dateOffset + 3],//hr
//                        data[dateOffset + 4],//min
//                        data[dateOffset + 5]//sec                
//                    );

//                    DateTime.TryParse(archiveRecordDateTimeString, out id.current);
//                    id.contractHour = data[dateOffset + 6];
//                }
//            }

//            return id;
//        }
//    }
//}
