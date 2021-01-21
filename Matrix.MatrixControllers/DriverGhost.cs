using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Matrix.MatrixControllers
{
    class DriverGhost
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Export("log")]
        private Action<string> logWrapper;
        public Action<string> Log { get; set; }

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

        [Export("setTimeDifference")]
        private Action<TimeSpan> setTimeDifferenceWrapper;
        public Action<TimeSpan> SetTimeDifference { get; set; }

        [Import("do")]
        public Func<string, dynamic, dynamic> Doing { get; private set; }

        public DriverGhost(AssemblyCatalog catalog)
        {
            logWrapper = (m) => Log(m);
            requestWrapper = (b) => Request(b);
            responseWrapper = () => Response();
            recordsWrapper = (r) => Records(r);
            cancelWrapper = () => Cancel();
            getLastTimeWrapper = (t) => GetLastTime(t);
            getLastRecordsWrapper = (t) => GetLastRecords(t);
            getRangeWrapper = (t, s, e) => GetRange(t, s, e);
            setContractHourWrapper = (ch) => SetContractHour(ch);
            getContractHourWrapper = () => GetContractHour();
            setTimeDifferenceWrapper = (ts) => SetTimeDifference(ts);

            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }
    }
}
