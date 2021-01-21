//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Domain.Entities;
//using Matrix.Common.Agreements;

//namespace Matrix.Common.Infrastructure.Pivot
//{
//    public static class Normalizer
//    {
//        /// <summary>
//        /// преобразует тотальные в ноткалькулятед
//        /// </summary>
//        /// <param name="data"></param>
//        /// <param name="factory"> </param>
//        /// <returns></returns>
//        public static IEnumerable<TParameterData> CalculateTotals<TParameterData>(IEnumerable<TParameterData> data, IEnumerable<TubeParameter> parameters, Func<TParameterData, double, string, TParameterData> factory) where TParameterData : DataRecord
//        {
//            if (data == null) return null;
//            var startSet = data.ToList();
//            var resultSet = new List<TParameterData>();

//            var tubesToSave = new List<Node>();

//            //группируем по тюбам
//            foreach (var parameterData in startSet.
//                GroupBy(d => d.ObjectId).Select(g => new
//                {
//                    TubeId = g.Key,
//                    Values = g.ToList()
//                }))
//            {

//                //Node tube = null;
//                //if (tube == null) continue;
//                var tubeParameters = parameters.Where(tp => tp.TubeId == parameterData.TubeId);
//                var newParameters = new List<TubeParameter>();
//                foreach (var parameter in tubeParameters)
//                {
//                    if (parameter.CalculationType == CalculationType.Total)
//                    {
//                        var virtualParameter = tubeParameters.FirstOrDefault(tp => tp.SystemParameterId == parameter.SystemParameterId + "Виртуальный" && tp.IsVirtual);
//                        if (virtualParameter == null)
//                        {
//                            virtualParameter = new TubeParameter()
//                            {
//                                CalculationType = CalculationType.NotCalculated,
//                                Id = Guid.NewGuid(),
//                                IsVirtual = true,
//                                MeasuringUnitId = parameter.MeasuringUnitId,
//                                SystemParameterId = parameter.SystemParameterId + "Виртуальный",
//                                CalculationTypeId = CalculationType.NotCalculated.ToString(),
//                                TubeId = parameter.TubeId
//                            };
//                            virtualParameter.Tags = new List<Tag>();
//                            foreach (var tag in parameter.Tags)
//                            {
//                                virtualParameter.SetTag(tag.Name, tag.Value, tag.IsSpecial);
//                            }
//                            newParameters.Add(virtualParameter);
//                        }

//                        double previousValue = .0;
//                        bool firstRow = true;
//                        foreach (var day in parameterData.Values.Where(d => d.S1 == parameter.SystemParameterId).OrderBy(d => d.Date))
//                        {
//                            var newValue = day.D1 - previousValue;
//                            previousValue = day.D1.Value;
//                            if (firstRow)
//                            {
//                                firstRow = false;
//                            }
//                            else
//                            {

//                                var virtualDay = factory(day, newValue.Value, virtualParameter.SystemParameterId);
//                                resultSet.Add(day);
//                                resultSet.Add(virtualDay);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        resultSet.AddRange(parameterData.Values.Where(pd => pd.S1 == parameter.SystemParameterId));
//                    }
//                }

//                if (newParameters.Any())
//                {
//                    //tube.Parameters.AddRange(newParameters);
//                    //tubesToSave.Add(tube);
//                }
//            }
//            return resultSet;
//        }

//        public static IEnumerable<DataRecord> ReplaceTotals(IEnumerable<DataRecord> data, IEnumerable<TubeParameter> parameters)
//        {
//            if (data == null) return null;
//            var startSet = data.ToList();
//            var resultSet = new List<DataRecord>();

//            //группируем по тюбам
//            foreach (var parameterData in startSet.
//                GroupBy(d => d.ObjectId).Select(g => new
//                {
//                    TubeId = g.Key,
//                    Values = g.ToList()
//                }))
//            {
//                foreach (var parameter in parameters)
//                {
//                    if (parameter.CalculationType == CalculationType.Total)
//                    {
//                        double previousValue = .0;
//                        bool firstRow = true;
//                        foreach (var day in parameterData.Values.Where(d => d.S1 == parameter.SystemParameterId).OrderBy(d => d.Date))
//                        {
//                            var newValue = day.D1 - previousValue;
//                            previousValue = day.D1.Value;
//                            if (firstRow)
//                            {
//                                firstRow = false;
//                            }
//                            else
//                            {
//                                day.D1 = newValue;
//                                resultSet.Add(day);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        resultSet.AddRange(parameterData.Values.Where(pd => pd.S1 == parameter.SystemParameterId));
//                    }
//                }
//            }
//            return resultSet;
//        }

//        public static IEnumerable<TParameterData> CalculateOffsetAndKoeficient<TParameterData>(IEnumerable<TParameterData> data, ICache cache, SessionUser user) where TParameterData : DataRecord
//        {
//            if (data == null) return null;
//            var resultSet = data.ToList();
//            foreach (var parameterData in resultSet.
//                GroupBy(d => d.ObjectId).Select(d => new
//                {
//                    TubeId = d.Key,
//                    Values = d.ToList()
//                }))
//            {
//                var tube = cache.ById(parameterData.TubeId, user) as Node;
//                //foreach (var parameter in tube.Parameters)
//                foreach (var parameter in new List<TubeParameter>())
//                {
//                    var k = parameter.GetDoubleTag("множитель") ?? 1.0;
//                    var o = parameter.GetDoubleTag("начальное значение") ?? 0.0;
//                    foreach (var hour in parameterData.Values.Where(d => d.ObjectId == tube.Id && d.S1 == parameter.SystemParameterId))
//                    {
//                        hour.D1 = hour.D1 * k + o;
//                    }
//                }
//            }
//            return resultSet;
//        }

//        ///// <summary>
//        ///// преобразует тотальные в ноткалькулятед
//        ///// </summary>
//        ///// <param name="data"></param>
//        ///// <returns></returns>
//        //public static IEnumerable<HourlyData> CalculateHourTotals(IEnumerable<HourlyData> data)
//        //{
//        //    var resultSet = data.ToList();
//        //    foreach (var parameterData in data.Where(d => d.CalculationType == CalculationType.Total).
//        //        GroupBy(d => new
//        //        {
//        //            d.TubeId,
//        //            d.SystemParameterId,
//        //            d.Channel,
//        //            d.CalculationTypeId
//        //        }))
//        //    {
//        //        double previousValue = 0.0;
//        //        foreach (var day in parameterData.OrderBy(d => d.DateStart))
//        //        {
//        //            var notNoramalData = resultSet.FirstOrDefault(d => d.Id == day.Id);
//        //            resultSet.Remove(notNoramalData);
//        //            var newValue = day.Value - previousValue;
//        //            previousValue = day.Value;
//        //            day.Value = newValue;
//        //            resultSet.Add(day);
//        //        }
//        //    }
//        //    return resultSet;
//        //}

//        public static void DecorateDays()
//        {

//        }
//    }
//}
