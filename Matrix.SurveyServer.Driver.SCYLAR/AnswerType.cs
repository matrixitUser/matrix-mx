using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.SCYLAR
{
    enum AnswerType
    {
        Success,
        Timeout,
        Error
    }
    static class AnswertTypeExtension
    {
        public static SurveyResultState ToSurveyResultState(this AnswerType answerType)
        {
            switch (answerType)
            {
                case AnswerType.Success:
                    return SurveyResultState.Success;
                case AnswerType.Timeout:
                    return SurveyResultState.NoResponse;
                case AnswerType.Error:
                    return SurveyResultState.NotRecognized;
            }
            return SurveyResultState.NoResponse;
        }
    }
}
