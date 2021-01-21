using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus3
{
    interface ITrack
    {
        Request GetRequest(byte networkAddress, int inx);
        DateTime GetDate(byte[] rsp, Passport passport);
        List<Data> GetData(byte[] rsp, Passport passport);
        int GetOffset(DateTime a, DateTime b);
        int GetCapacity(Passport passport);
    }
}
