using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.TSRV024
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

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;
    }
}
