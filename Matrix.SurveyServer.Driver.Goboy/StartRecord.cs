using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        private dynamic GetStartRecord(int sn, short start)
        {
            return ParseStartRecord(Send(MakeMemoryRequest(sn, start, 6)));
        }

        private dynamic ParseStartRecord(byte[] bytes)
        {
            var start = ParseResponse(bytes);
            if (!start.success)
            {
                return start;
            }

            if (start.body.Length < 6)
            {
                start.success = false;
                start.error = "длина пакета с датой не может быть меньше 6";
                return start;
            }

            start.date = new DateTime(
                2000 + start.body[5],
                start.body[4],
                start.body[3],
                start.body[2],
                start.body[1],
                start.body[0]
            );

            return start;
        }
    }
}
