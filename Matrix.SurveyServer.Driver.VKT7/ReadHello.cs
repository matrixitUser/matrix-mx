using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
    /// <summary>
    /// начало сеанса связи
    /// см. док. п. 4.7 стр. 18
    /// </summary>
    public partial class Driver
    {
        byte[] MakeReadHelloRequest()
        {
            var Frame = new List<byte>();
            Frame.Add(0x3f);
            Frame.Add(0xff);
            Frame.Add(0x00);
            Frame.Add(0x00);
            Frame.Add(0xcc);
            Frame.Add(0x80);
            Frame.Add(0x00);
            Frame.Add(0x00);
            Frame.Add(0x00);
            return MakeBaseRequest(0x10, Frame);
        }
    }
}
