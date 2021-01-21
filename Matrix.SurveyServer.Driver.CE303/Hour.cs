using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.CE303
{
    public partial class Driver
    {
        dynamic GetHours(DateTime start, DateTime end, DateTime dtcounter)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            archive.records = new List<dynamic>();

            var dtcounterH = dtcounter.Date.AddHours(dtcounter.Hour);

            //Модель
            var model = ParseResponse(Send(MakeDataRequest("MODEL()")));
            if (!model.success) return model;

            //диапазон усреднения в минутах
            var taver = ParseResponse(Send(MakeDataRequest("TAVER()")));
            if (!taver.success) return taver;

            archive.tAver = System.Int32.Parse(DriverHelper.Parsing("TAVER", taver.rsp)[0]);

            //Разрешение перехода на летнее время
            var trsum = ParseResponse(Send(MakeDataRequest("TRSUM()")));
            if (!trsum.success) return trsum;

            //Состояние счетчика
            var stat = ParseResponse(Send(MakeDataRequest("STAT_()")));
            if (!stat.success) return stat;

            //Дата создания профиля нагрузки 25 го часа (переходного при переходе на зимнее время)
            var dat25 = ParseResponse(Send(MakeDataRequest("DAT25()")));
            if (!dat25.success) return dat25;

            //Считывание мощности
            //Каждый час опрашиваем отдельно, ориентируясь на диапазон усреднения мощности tAver,
            //  то есть суммируя диапазоны
            int nDiapasone = 60 / archive.tAver; // число дипазонов внутри часа и он же коээфициент на который надо разделить мощности по диапазонам

            //Считывание мощностей 

            var date = start.Date.AddHours(start.Hour);
            while (date < end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    archive.errorcode = DeviceError.NO_ERROR;
                    break;
                }

                if (date >= dtcounterH)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    break;
                }

                var recs = new List<dynamic>();
                //log(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm} ", date));

                var grape = ParseGraxxResponse(Send(MakeDataRequest(string.Format("GRAPE({0:dd.MM.yy}.{1}.{2})", date, (date.Hour * nDiapasone + 1), nDiapasone))), "GRAPE", date, archive.tAver);
                if ((!grape.success) && (grape.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return grape;
                if (grape.success) recs.AddRange(grape.records);

                var grapi = ParseGraxxResponse(Send(MakeDataRequest(string.Format("GRAPI({0:dd.MM.yy}.{1}.{2})", date, (date.Hour * nDiapasone + 1), nDiapasone))), "GRAPI", date, archive.tAver);
                if ((!grapi.success) && (grapi.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return grapi;
                if (grapi.success) recs.AddRange(grapi.records);

                var graqe = ParseGraxxResponse(Send(MakeDataRequest(string.Format("GRAQE({0:dd.MM.yy}.{1}.{2})", date, (date.Hour * nDiapasone + 1), nDiapasone))), "GRAQE", date, archive.tAver);
                if ((!graqe.success) && (graqe.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return graqe;
                if (graqe.success) recs.AddRange(graqe.records);


                var graqi = ParseGraxxResponse(Send(MakeDataRequest(string.Format("GRAQI({0:dd.MM.yy}.{1}.{2})", date, (date.Hour * nDiapasone + 1), nDiapasone))), "GRAQI", date, archive.tAver);
                if ((!graqi.success) && (graqi.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return graqi;
                if (graqi.success) recs.AddRange(graqi.records);

                int count = recs.Count;
                //log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm} P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}", date, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));
                log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm} прочитаны", date));

                records(recs);

                archive.records.AddRange(recs);

                date = date.AddHours(1);
            }


            //log(string.Format("Часовые Q+ ({0}):", hours.Count));
            //foreach (var data in hours)
            //{
            //    if (data.s1 == "Q+")
            //    {
            //        log(string.Format("{0:dd.MM.yyyy HH:mm} {1} = {2} {3}", data.date, data.s1, data.d1, data.s2));
            //    }
            //}
            return archive;
        }

    }

}
