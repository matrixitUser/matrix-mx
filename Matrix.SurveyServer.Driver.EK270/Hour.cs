using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private dynamic GetHours(DateTime start, DateTime end, DevType devType, float version)
        {
            dynamic archive = new ExpandoObject();
            archive.records = new List<dynamic>();

            //var head = "date\tGONo\tAONo\tVb\tVbT\tV\tVo\tpMP\tTMP\tKMP\tCMP\tT2Tek\tSt2\tSt4\tSt7\tSt6\tStSy";
            //  log(head);

            var startHour = start.Date.AddHours(start.Hour);

            for (DateTime date = startHour; date < end; date = date.AddHours(1))
            {
                dynamic hour = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel())
                    {
                        archive.success = false;
                        archive.error = "опрос отменен";
                        return archive;
                    }

                    var dt = date.AddHours(1);
                    hour = GetArchiveRecord(dt, devType, version);
                    if (hour.success) break;
                }

                if (!hour.success)
                {
                    log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", date, hour.error));
                    return hour;
                }

                log(string.Format("часовая запись {0:dd.MM.yy HH:mm} получена", date));
                records(hour.records);
                archive.records.AddRange(hour.records);
            }

            archive.success = true;
            archive.error = string.Empty;
            return archive;
        }
    }
}
