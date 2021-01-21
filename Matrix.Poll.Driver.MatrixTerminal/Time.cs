using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MatrixTerminal
{
    public partial class Driver
    {
        public dynamic CorrectTime()
        {
            log(string.Format("Корректировка времени !!!!"), level: 1);

            var bkp = Send(MakeWriteBkpRequest(DateTime.Now));
            return bkp;
            //var time = ParseTimeResponse(Send(MakeTimeRequest()));
            //if (!time.success)
            //{
            //    return MakeResult(101, time.errorcode, time.error);
            //}

            //DateTime date = time.date;

            //DateTime now = DateTime.Now;
            //var timeOffset = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
            //bool isSetTime = timeOffset > 0; //5;  20190404  
            //// коррекция времени (елси отличается больше, чем на 5 секунд и если время опроса соответствует HH:04-HH:56)
            //if (isSetTime)
            //{
            //    var bkp = Send(MakeWriteBkpRequest(DateTime.Now));
            //    if (bkp.success)
            //    {
            //        time = ParseTimeResponse(Send(MakeTimeRequest()));
            //        if (!time.success) return time;
            //    }
            //    var timeOffsetNew = ((time.date > DateTime.Now) ? (time.date - DateTime.Now).TotalSeconds : (DateTime.Now - time.date).TotalSeconds);
            //    if (bkp.success && time.success)
            //    {
            //        date = time.date;
            //        log(string.Format(isSetTime ? "Время установлено" : "Произведена корректировка времени на {0:0.###} секунд", timeOffset), level: 1);
            //    }
            //    else
            //    {
            //        log(string.Format("Время НЕ {0}: {1}", isSetTime ? "установлено" : "скорректировано", bkp.success ? time.error : bkp.error), level: 1);
            //    }
            //}
            //else
            //{
            //    log(string.Format("Корректировка времени не требуется"), level: 1);
            //}
            //return time;
        }

        byte[] MakeTimeRequest()
        {
            return MakeRegisterRequest(0x32000, 4);
        }
        
        dynamic ParseTimeResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            var now = DateTime.Now;
            
            var rsp = ParseRegisterResponse(answer);
            if(!rsp.success) return rsp;

            if (((Array)rsp.Register).Length > 4)
            {
                rsp.date = new DateTime(2000 + rsp.Register[0], rsp.Register[1], rsp.Register[2], rsp.Register[3], rsp.Register[4], rsp.Register[5]);
            }
            else
            {
                var sec = Helper.ToUInt32(rsp.Register, 0);
                rsp.date = new DateTime(1970, 1, 1).AddSeconds(sec);
            }

            rsp.now = now;

            return rsp;
        }
    }
}
