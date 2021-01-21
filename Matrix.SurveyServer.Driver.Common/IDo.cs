using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
    /// <summary>
    /// позволяет выполнить любую операцию
    /// </summary>
    public interface IDo
    {
        void Do(string what, IDictionary<string, object> argument);
        event EventHandler<DoEventArgs> DoEvent;
    }

    public class DoEventArgs : EventArgs
    {
        public IEnumerable<Record> Records { get; private set; }

        public DoEventArgs(IEnumerable<Record> records)
        {
            Records = records;
        }
    }

    public class Record
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public double? D1 { get; set; }
        public double? D2 { get; set; }
        public double? D3 { get; set; }
        public string S1 { get; set; }
        public string S2 { get; set; }
        public string S3 { get; set; }
        public DateTime? Dt1 { get; set; }
        public DateTime? Dt2 { get; set; }
        public DateTime? Dt3 { get; set; }
        public int? I1 { get; set; }
        public int? I2 { get; set; }
        public int? I3 { get; set; }
    }    
}
