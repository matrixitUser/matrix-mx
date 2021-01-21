using System.Collections.Generic;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.Common
{
    public class SurveyResult
    {
        public SurveyResultState State { get; set; }
    }
    public class SurveyResultAbnormalEvents:SurveyResult
    {
        public IEnumerable<AbnormalEvents> Records { get; set; }
    }
    public class SurveyResultData : SurveyResult
    {
        public IEnumerable<Data> Records { get; set; }
    }
    public class SurveyResultConstant : SurveyResult
    {
        public IEnumerable<Constant> Records { get; set; }
    }
}
