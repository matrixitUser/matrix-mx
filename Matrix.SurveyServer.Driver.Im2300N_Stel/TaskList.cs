using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetTaskList(byte na, DateTime date)
        {
            return ParseTaskList(GetBlocks(na, 0x9A, 1, 1, 258), date);
        }

        private dynamic ParseTaskList(byte[] bytes, DateTime date)
        {
            dynamic tasks = new ExpandoObject();

            if (bytes == null || !bytes.Any())
            {
                tasks.success = false;
                tasks.error = "нет данных для разбора";
                return tasks;
            }

            tasks.records = new List<dynamic>();
            //var list = new List<string>();
            //log(string.Join(",", bytes.Select(b => b.ToString("X2"))));
            for (var i = 255; i > 0; i--)
            {
                var task = bytes[i];
                if (task != 0x00)
                {
                    tasks.records.Add(MakeConstRecord("задача", task, date));
                    tasks.task = task;
                    //list.Add(task.ToString());
                    break;
                }
            }
            //tasks.records.Add(MakeConstRecord("задачи", string.Join(",", list), date));
            //log(string.Join(",", list));
            tasks.success = true;
            return tasks;
        }
    }
}
