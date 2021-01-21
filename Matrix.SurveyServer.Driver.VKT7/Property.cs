using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        dynamic GetProperties(int serverVersion)
        {
            dynamic answer = new ExpandoObject();
            answer.error = string.Empty;
            answer.success = true;

            if (cancel())
            {
                answer.success = false;
                answer.error = "опрос отменен";
                return answer;
            }

            //Запись типа значение с номером 6 («свойства»);
            var write = ParseWriteResponse(Send(MakeWriteValueTypeRequest(ValueType.Properties)));
            if (!write.success) return write;

            //2. Запись перечня элементов для чтения
            var unitElements = new List<dynamic>();

            //+44 tTypeM ед. измерения по t (температуре) свойство
            //+45 GTypeM ед. измерения по G (расходу) свойство
            //+46 VTypeM ед. измерения по V (объему) свойство
            //+47 MTypeM ед. измерения по M (массе) свойство
            //+48 PTypeM ед. измерения по P (давлению) свойство
            //49 dtTypeM ед. измерения по dt (температуре) свойство
            //50 tswTypeM ед. измерения по tx (температуре) свойство
            //51 taTypeM ед. измерения по ta (температуре) свойство
            //52 MgTypeM ед. измерения по M (массе) свойство
            //+53 QoTypeM ед. измерения по Q (теплу) свойство
            //54 QgTypeM ед. измерения по Q (теплу) свойство
            //+55 QntTypeHIM ед. измерения по BНP (времени) свойство
            //+56 QntTypeM ед. измерения по ВОС (времени)* (см. прим.) свойство
            foreach (var i in new byte[] { 44, 45, 46, 47, 48, 53, 55, 56 })
            {
                dynamic element = new ExpandoObject();
                element.Address = i;
                element.Length = 7;
                unitElements.Add(element);
            }

            write = ParseWriteResponse(Send(MakeWriteElementsRequest(unitElements)));
            if (!write.success) return write;

            //3. Чтение данных в соответствии с записанным перечнем. При получении
            //ответа на этот запрос анализировать байты качества и нештатных си-
            //туаций не нужно.
            var units = ParseReadPropertiesUnitsResponse(Send(MakeReadPropertiesUnitsRequest()), unitElements, serverVersion);
            if (!units.success) return units;


            //2. Запись перечня элементов для чтения
            var mulElements = new List<dynamic>();

            //+57 tTypeFractDigNum кол-во знаков после запятой для t свойство
            //58 GTypeFractDigNum1 резерв свойство
            //+59 VTypeFractDigNum1 кол-во знаков после запятой для V по Тв1 свойство
            //+60 MTypeFractDigNum1 кол-во знаков после запятой для M по Тв1 свойство
            //+61 PTypeFractDigNum1 кол-во знаков после запятой для P свойство
            //62 dtTypeFractDigNum1 кол-во знаков после запятой для dt свойство
            //63 tswTypeFractDigNum1 кол-во знаков после запятой для tx свойство
            //64 taTypeFractDigNum1 кол-во знаков после запятой для ta свойство
            //65 MgTypeFractDigNum1 кол-во знаков после запятой для Mг свойство
            //66 QoTypeFractDigNum1 кол-во знаков после запятой для Q по Тв1 свойство
            //67 tTypeFractDigNum2 резерв свойство
            //68 GTypeFractDigNum2 резерв свойство
            //+69 VTypeFractDigNum2 кол-во знаков после запятой для V по Тв2 свойство
            //+70 MTypeFractDigNum2 кол-во знаков после запятой для M по Тв2 свойство
            //71 PTypeFractDigNum2 кол-во знаков после запятой для P свойство
            //72 dtTypeFractDigNum2 кол-во знаков после запятой для dt свойство
            //73 tswTypeFractDigNum2 кол-во знаков после запятой для tx свойство
            //74 taTypeFractDigNum2 кол-во знаков после запятой для ta свойство
            //75 MgTypeFractDigNum2 кол-во знаков после запятой для Mг свойство
            //+76 QoTypeFractDigNum2 кол-во знаков после запятой для Q по Тв2 свойство
            foreach (var i in new byte[] { 57, 59, 60, 61, 69, 70, 76 })
            {
                dynamic element = new ExpandoObject();
                element.Address = i;
                element.Length = 1;
                mulElements.Add(element);
            }

            write = ParseWriteResponse(Send(MakeWriteElementsRequest(mulElements)));
            if (!write.success) return write;

            var fracs = ParseReadPropertiesMultiplierResponse(Send(MakeReadPropertiesFracsRequest()), mulElements);
            if (!fracs.success) return fracs;

            answer.Units = units.Units;
            answer.Fracs = fracs.Fracs;

            return answer;
        }
    }
}
