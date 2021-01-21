using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;
using System.ComponentModel.Composition;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

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

        [Import("setContractDay")]
        private Action<int> setContractDay;
    }
}
