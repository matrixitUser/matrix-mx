using log4net;
using Matrix.PollServer.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    class DriverGhost
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DriverGhost));

        //deprecated
        [Export("log")]
        private Action<string> logWrapper;
        public Action<string> Log { get; set; }
        
        [Export("logger")]
        private Action<string, int> loggerWrapper;
        public Action<string, int> Logger { get; set; }

        [Export("request")]
        private Action<byte[]> requestWrapper;
        public Action<byte[]> Request { get; set; }

        [Export("response")]
        private Func<byte[]> responseWrapper;
        public Func<byte[]> Response { get; set; }

        [Export("records")]
        private Action<IEnumerable<dynamic>> recordsWrapper;
        public Action<IEnumerable<dynamic>> Records { get; set; }

        [Export("cancel")]
        private Func<bool> cancelWrapper;
        public Func<bool> Cancel { get; set; }

        [Export("getLastTime")]
        private Func<string, DateTime> getLastTimeWrapper;
        public Func<string, DateTime> GetLastTime { get; set; }

        [Export("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecordsWrapper;
        public Func<string, IEnumerable<dynamic>> GetLastRecords { get; set; }

        [Export("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRangeWrapper;
        public Func<string, DateTime, DateTime, IEnumerable<dynamic>> GetRange { get; set; }

        [Export("getContractHour")]
        private Func<int> getContractHourWrapper;
        public Func<int> GetContractHour { get; set; }

        [Export("setContractHour")]
        private Action<int> setContractHourWrapper;
        public Action<int> SetContractHour { get; set; }

        [Export("getContractDay")]
        private Func<int> getContractDayWrapper;
        public Func<int> GetContractDay { get; set; }

        [Export("setContractDay")]
        private Action<int> setContractDayWrapper;
        public Action<int> SetContractDay { get; set; }

        [Export("setModbusControl")]
        private Action<dynamic> setModbustControlWrapper;
        public Action<dynamic> SetModbusControl { get; set; }

        [Export("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCacheWrapper;
        public Action<double, string, DateTime> SetIndicationForRowCache { get; set; }

        [Export("setTimeDifference")]
        private Action<TimeSpan> setTimeDifferenceWrapper;
        public Action<TimeSpan> SetTimeDifference { get; set; }

        [Export("recordLoad")]
        private Func<DateTime, DateTime, string, List<dynamic>> recordLoadWrapper;
        public Func<DateTime, DateTime, string, List<dynamic>> recordLoad { get; set; }

        [Export("recordLoadWithId")]
        private Func<DateTime, DateTime, string, Guid, List<dynamic>> recordLoadWithIdWrapper;
        public Func<DateTime, DateTime, string, Guid, List<dynamic>> recordLoadWithId { get; set; }
        
        [Export("loadRowsCache")]
        private Func<Guid, List<dynamic>> loadRowsCacheWrapper;
        public Func<Guid, List<dynamic>> loadRowsCache { get; set; }

        [Export("loadRecordsPowerful")]
        private Func<DateTime, DateTime, string, string,string, List<dynamic>> loadRecordsPowerfulWrapper;
        public Func<DateTime, DateTime, string,string,string, List<dynamic>> loadRecordsPowerful { get; set; }

        [Export("setArchiveDepth")]
        private Action<string, int> setArchiveDepthWrapper;
        public Action<string, int> SetArchiveDepth { get; set; }

        [Export("getArchiveDepth")]
        private Func<string, int> getArchiveDepthWrapper;
        public Func<string, int> GetArchiveDepth { get; set; }

        [Import("do")]
        public Func<string, dynamic, dynamic> Doing { get; private set; }
   
        public DriverGhost(AssemblyCatalog catalog)
        {
            logWrapper = (m) => Log(m);
            loggerWrapper = (m, l) => Logger(m, l);
            requestWrapper = (b) => Request(b);
            responseWrapper = () => Response();
            recordsWrapper = (r) => Records(r);
            cancelWrapper = () => Cancel();
            getLastTimeWrapper = (t) => GetLastTime(t);
            getLastRecordsWrapper = (t) => GetLastRecords(t);
            getRangeWrapper = (t, s, e) => GetRange(t, s, e);
            setContractHourWrapper = (ch) => SetContractHour(ch);
            getContractHourWrapper = () => GetContractHour();
            setContractDayWrapper = (cd) => SetContractDay(cd);
            getContractDayWrapper = () => GetContractDay();
            setTimeDifferenceWrapper = (ts) => SetTimeDifference(ts);
            setArchiveDepthWrapper = (t, d) => SetArchiveDepth(t, d);
            getArchiveDepthWrapper = (t) => GetArchiveDepth(t);
            recordLoadWrapper = (a,b,c) => recordLoad(a,b,c);
            recordLoadWithIdWrapper = (a, b, c, d) => recordLoadWithId(a, b, c, d);
            loadRowsCacheWrapper = (a) => loadRowsCache(a);
            loadRecordsPowerfulWrapper = (a, b, c, d,e) => loadRecordsPowerful(a, b, c, d,e);
            setModbustControlWrapper = (control) => SetModbusControl(control);
            setIndicationForRowCacheWrapper = (a,b,c) => SetIndicationForRowCache(a,b,c);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

        }
    }
}
