﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;


namespace Matrix.Poll.Driver.Neva
{
    public static class DriverHelper
    {

        #region Parsing
        public static List<string> responceToParameters(string nameParameter, byte[] responce, int nParameter)
        {

            List<string> sValparameters = new List<string>();
            List<string> parameters = new List<string>();
            string responceS = Encoding.Default.GetString(responce); ;
            while (responceS.Contains(nameParameter))
            {
                int indexValBegin = responceS.IndexOf(nameParameter);
                int indexValEnd = responceS.IndexOf(")", indexValBegin);
                parameters.Add((responceS.Substring(indexValBegin, indexValEnd - indexValBegin + 1)));
                responceS = responceS.Substring(indexValEnd + 1);
            }

            foreach (var parameter in parameters)
            {
                sValparameters.Add(Parsing(nameParameter, Encoding.ASCII.GetBytes(parameter))[nParameter]);
            }

            return sValparameters;
        }



        //парсинг результатов в список строк со значениями
        public static List<string> Parsing(string nameParameter, byte[] responce)
        {
            List<string> result = new List<string>();
            string responceS = Encoding.Default.GetString(responce); ;
            if (responceS.Contains(nameParameter))
            {
                int indexValBegin = responceS.IndexOf(nameParameter) + nameParameter.Length + 1;
                int indexValEnd = responceS.IndexOf(")", indexValBegin);
                int indexDelimeter = responceS.IndexOf(",", indexValBegin, indexValEnd - indexValBegin);
                while (indexDelimeter >= 0)
                {
                    result.Add(responceS.Substring(indexValBegin, indexDelimeter - indexValBegin));

                    indexValBegin = indexDelimeter + 1;
                    indexDelimeter = responceS.IndexOf(",", indexValBegin, indexValEnd - indexValBegin);
                }
                result.Add(responceS.Substring(indexValBegin, indexValEnd - indexValBegin));
            }
            return result;
        }
        #endregion
        #region DateTime from Counter
        public static DateTime DateTimeFromCounter(byte[] DateCurr, byte[] TimeCurr)
        {
            string sDate = DriverHelper.Parsing("000902FF", DateCurr)[0];
            //sDate = sDate.Substring(0, 6) + "20" + sDate.Substring(6, 2);
            string sTime = DriverHelper.Parsing("000901FF", TimeCurr)[0];

            DateTime dt = DateTime.ParseExact(sDate + " " + sTime, "yyMMdd HHmmss", CultureInfo.InvariantCulture);
            return dt;
        }
        #endregion
        #region BCC
        public static byte ComputeВCC(IList<byte> bytes,int begBCC)
        {
            byte bcc = 0;
            for (int i = begBCC; i < bytes.Count; ++i)
            {
                bcc = (byte)(bcc + bytes[i]);
            }

            return bcc;
        }
}
        #endregion

}

