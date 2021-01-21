using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private dynamic GetCurrent(bool? isConsumerCastle)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.records = new List<dynamic>();

            var time = ParseResponse(Send(MakeRequest(RequestType.Read, "1:0400.0", "1")));

            if (!time.success)
            {
                if ((time as IDictionary<string, object>).ContainsKey("errorCode") && time.errorCode == 18)
                {
                    var castle = OpenConsumerCastle(isConsumerCastle != false);
                    if(!castle.success)
                    {
                        return castle;
                    }
                }
                else
                    return time;
            }
            //if (time.ignore)
            //{
            //    log(string.Format("вычислитель игнорировал запрос о текущем времени, используется время сервера"));
            //    current.date = DateTime.Now;
            //}
            try
            {
                current.date = ParseDate(time.Values[0]);
            }
            catch (Exception ex)
            {
                current.success = false;
                current.error = ex.Message;
                return current;
            }

            foreach (var unit in currentValue)
            {
                if (cancel())
                {
                    current.success = false;
                    current.error = string.Format("отмена опроса");
                    return current;
                }

                var c = ParseSingleValueResponse(Send(MakeSingleValueRequest(unit.Address)));
                //if (!c.success) return c;
                if (!c.success) continue;

                if (unit.Address == "6:310_1.0")
                {
                    current.gasTemp = MakeCurrentRecord(unit.Description, c.Value, unit.MeasureUnit, current.date);
                }
                if (unit.Address == "7:310.0")
                {
                    current.gasPrs = MakeCurrentRecord(unit.Description, c.Value, unit.MeasureUnit, current.date);
                }

                if (c.isExist)
                {
                    current.records.Add(MakeCurrentRecord(unit.Description, c.Value, unit.MeasureUnit, current.date));
                }
                else
                {
                    log(string.Format("Мгновенное значение '{0}' в данном счетчике не представлено", unit.Description));
                }

            }

            return current;
        }

        #region
        private readonly List<MappingUnit> currentValue = new List<MappingUnit>
        {            
            //new MappingUnit
            //{
            //    Address = "2:300.0",
            //    Description = "Стандартный объем",
            //    Parameter = "VolumeNormal",
            //    MeasureUnit = "м³"
            //},
            new MappingUnit
            {
                Address = "2:301.0",
                Description = "Стандартный объем возмущенный",
                Parameter = "VolumeNormal",
                MeasureUnit = "м³"
            },
            new MappingUnit
            {
                Address = "2:302.0",
                Description = "Стандартный объем общий",
                Parameter = "VolumeNormal",
                MeasureUnit = "м³"
            },
            new MappingUnit
            {
                Address = "2:310.0",
                Description = "Стандартный расход",
                Parameter = "ConsamptionNormal",
                MeasureUnit = "м³/ч"
            },

            // new MappingUnit
            //{
            //    Address = "4:300.0",
            //    Description = "Рабочий объем",
            //    Parameter = "VolumeNormal",
            //    MeasureUnit = "м³"
            //},

            new MappingUnit
            {
                Address = "4:301.0",
                Description = "Рабочий объем возмущенный",
                Parameter = "VolumeNormal",
                MeasureUnit = "м³"
            },
            new MappingUnit
            {
                Address = "4:302.0",
                Description = "Рабочий объем общий",
                Parameter = "VolumeNormal",
                MeasureUnit = "м³"
            },

            new MappingUnit
            {
                Address = "1:210.0",
                Description = "Рабочий расход",
                Parameter = "ConsamptionWork",
                MeasureUnit = "м³/ч"
            },

            new MappingUnit
            {
                Address = "4:310.0",
                Description = "Рабочий расход",
                Parameter = "ConsamptionWork",
                MeasureUnit = "м³/ч"
            },
            new MappingUnit
            {
                Address = "6:310_1.0",
                Description ="Температура газа T",
                Parameter = "Temperature",
                MeasureUnit = "°C"
            },
            new MappingUnit
            {
                Address = "7:310.0",
                Description ="Давление газа P",
                Parameter = "Pressure",
                MeasureUnit = "бар"
            },
            // new MappingUnit
            //{
            //    Address = "5:310.0",
            //    Description ="Коэффициент коррекции",
            //    Parameter = "Pressu1",
            //    MeasureUnit = ""
            //},
            //new MappingUnit
            //{
            //    Address = "8:310.0",
            //    Description ="Коэффициент сжимаемости",
            //    Parameter = "Pressu2",
            //    MeasureUnit = ""
            //},
            new MappingUnit
            {
                Address = "2:404.0",
                Description ="Остаток питания",
                Parameter = "Pressu3",
                MeasureUnit = "мес"
            }
        };
        #endregion
    }
}
