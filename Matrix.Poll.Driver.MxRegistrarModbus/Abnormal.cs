using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        private dynamic ReadAbnormalByInx(int inx, out DateTime cDate)
        {
            var response = Parse65AbnormalResponse(Send(Make65Request(ArchiveType.Abnormal, (UInt16)inx)));
            cDate = response.Date;
            return response;
        }


        private dynamic GetAbnormals(int count = 1, DateTime? start0 = null)
        {
            var start = start0 == null ? DateTime.MinValue : (DateTime)start0;

            dynamic archive = new ExpandoObject();
            archive.success = false;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var abnormals = new List<dynamic>();

            for (var i = 0; i < count; i++)
            {
                var inx = i;
                if (cancel())
                {
                    //archive.success = false;
                    archive.errorcode = DeviceError.NO_ERROR;
                    archive.error = "опрос отменен";
                    return archive;
                }

                DateTime date;
                var record = ReadAbnormalByInx(inx, out date);

                if (date == DateTime.MinValue)
                {
                    log(string.Format("События #{0} нет в архиве", inx));
                    break;
                }

                if (!record.success)
                {
                    archive.error = string.Format("Не удалось прочитать событие {0}: {1}", inx, record.error);
                    archive.errorcode = record.errorcode;
                    break;
                }

                log(string.Format("Событие на {0}: {1}", date, record.eventDescription));

                if (date <= start)
                {
                    break;
                }

                abnormals.Add(MakeAbnormalRecord(record.eventDescription, 0, date));
            }

            records(abnormals);

            archive.success = true;
            archive.records = abnormals;
            return archive;
        }
    }




}
