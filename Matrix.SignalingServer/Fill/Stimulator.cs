using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Matrix.SignalingServer.Fill
{
    class Stimulator : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private Stimulator() { }

        private static List<JobRule> rules = new List<JobRule>()
        {
        };
        
        static Stimulator()
        {
            rules.Add(new JobRule("опросы", ConfigurationManager.AppSettings["cron"], PollJob));
        }

        public void Reload()
        {
        }

        public void Load()
        {
        }

        private static readonly Stimulator instance = new Stimulator();
        public static Stimulator Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            foreach (var rule in rules)
            {
                rule.Dispose();
            }
        }


        private static void PollJob()
        {
            log.Info("задание \"опросы\" сработало");
            
        }
    }


    class JobRule : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private string name;
        private string cron;
        private Action jobTodo;

        System.Threading.Timer timer;

        public JobRule(string name, string cron, Action jobTodo)
        {
            this.name = name;
            this.cron = cron;
            this.jobTodo = jobTodo;

            var now = DateTime.Now;
            var next = NCrontab.CrontabSchedule.Parse(cron).GetNextOccurrence(now);

            log.Info("создано правило рассылки {0}, следующее срабатывание {1:dd.MM.yy HH:mm:ss.fff}", name, next);
            timer = new System.Threading.Timer(DoIt, null, next - now, new TimeSpan(-1));
        }

        void DoIt(object state)
        {
            jobTodo?.Invoke();

            var now = DateTime.Now;
            var next = NCrontab.CrontabSchedule.Parse(cron).GetNextOccurrence(now);
            log.Debug("задача {0} сработает {1:dd.MM.yy HH:mm:ss}", name, next);
            timer = new System.Threading.Timer(DoIt, null, next - now, new TimeSpan(-1));
        }

        public void Dispose()
        {
            if (timer != null)
            {
                log.Debug("таймер для {0} остановлен", name);
                timer.Dispose();
                timer = null;
            }
        }
    }
}
