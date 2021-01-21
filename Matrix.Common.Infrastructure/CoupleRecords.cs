using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Matrix.Common.Infrastructure
{
    public class CoupleRecords<TRecord>
    {
        public int Timeout { get; set; }
        public int MaxCount { get; set; }

        private Timer sendTimer = new Timer();

        private List<TRecord> elements = new List<TRecord>();


        public CoupleRecords(int timeout, int maxCount)
        {
            Timeout = timeout;
            MaxCount = maxCount;
            sendTimer.Interval = Timeout;
            sendTimer.AutoReset = false;
            sendTimer.Elapsed += (se, ea) =>
            {
                lock (elements)
                {
                    if (elements.Count > 0)
                    {
                        RaiseCoupleRecordsReady(elements);
                        elements.Clear();
                    }
                }
            };
        }

        public CoupleRecords()
            : this(60000, 100)
        {
        }

        public void Add(TRecord record)
        {
            Add(new TRecord[] { record });
        }

        public void Add(IEnumerable<TRecord> records)
        {
            lock (elements)
            {
                var isFirstTime = elements.Count == 0;
                elements.AddRange(records);

                if (elements.Count >= MaxCount)
                {
                    RaiseCoupleRecordsReady(elements);
                    elements.Clear();
                    return;
                }
                if (isFirstTime)
                {
                    sendTimer.Start();
                    return;
                }
            }
        }

        public event EventHandler<CoupleRecordsReadyEventArgs<TRecord>> CoupleRecordsReady;

        private void RaiseCoupleRecordsReady(IEnumerable<TRecord> records)
        {
            sendTimer.Stop();
            if (CoupleRecordsReady != null)
            {
                CoupleRecordsReady(this, new CoupleRecordsReadyEventArgs<TRecord>(records));
            }
        }
    }

    public class CoupleRecordsReadyEventArgs<TRecord> : EventArgs
    {
        public IEnumerable<TRecord> Records { get; private set; }

        public CoupleRecordsReadyEventArgs(IEnumerable<TRecord> records)
        {
            Records = records;
        }
    }
}
