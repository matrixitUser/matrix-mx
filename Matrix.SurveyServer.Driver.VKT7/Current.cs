﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        dynamic GetCurrents(dynamic properties, DateTime currentDt)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;

            if (cancel())
            {
                current.success = false;
                current.error = "опрос отменен";
                return current;
            }

            var recs = new List<dynamic>();

            current.date = currentDt;


            var write = ParseWriteResponse(Send(MakeWriteValueTypeRequest(ValueType.Current)));
            if (!write.success) return write;

            var elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            if (!elements.success) return elements;


            var filterElements = FilterElements((List<dynamic>)elements.ActiveElements, ValueType.Current);

            write = ParseWriteResponse(Send(MakeWriteElementsRequest(filterElements)));
            if (!write.success) return write;

            var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), currentDt, properties.Fracs, properties.Units, filterElements, ValueType.Current);
            if (!data.success) return data;

            recs.AddRange(data.Data);
            
            
            write = ParseWriteResponse(Send(MakeWriteValueTypeRequest(ValueType.TotalCurrent)));
            if (!write.success) return write;

            elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            if (!elements.success) return elements;

            filterElements = FilterElements((List<dynamic>)elements.ActiveElements, ValueType.TotalCurrent);

            write = ParseWriteResponse(Send(MakeWriteElementsRequest(filterElements)));
            if (!write.success) return write;

            data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), currentDt, properties.Fracs, properties.Units, filterElements, ValueType.TotalCurrent);
            if (!data.success) return data;

            recs.AddRange(data.Data);

            current.records = recs;
            return current;
        }

    }
}