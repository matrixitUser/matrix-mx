//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Reflection;
//using log4net;
//using Matrix.Common.Agreements;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{
//    public static class AggregationRootHelper
//    {
//        private readonly static ILog log = LogManager.GetLogger(typeof(AggregationRootHelper));

//        public static string GetFullName(ICache cache, Guid objectId, SessionUser user, int variant)
//        {
//            var entity = cache.ById(objectId, user);
//            if (entity == null) return "";

//            if (entity is Node)
//            {
//                var node = entity as Node;
//                if (node.Type == "Area")
//                {

//                    return string.Format("{0} {1} {2} {3}", node.GetStringTag("name"),
//                        node.GetStringTag("city"), node.GetStringTag("street"), node.GetStringTag("house"));
//                }

//                if (node.Type == "Tube")
//                {
//                    string name = "";
//                    //var tube = entity as Tube;

//                    if (variant == 1)
//                    {
//                        var rel = cache.Get<Relation>(r => r.EndNodeId == node.Id, user).FirstOrDefault();
//                        if (rel != null)
//                        {
//                            name = string.Format("{0} {1}", name, GetFullName(cache, rel.StartNodeId, user, 0));
//                        }
//                    }

//                    name = string.Format("{0}{1} ", name, node.GetStringTag("name"));

//                    if (node.GetGuidTag("deviceTypeId") != null)
//                    {
//                        var deviceType = cache.ById(node.GetGuidTag("deviceTypeId").Value, user) as DeviceType;
//                        if (deviceType != null)
//                        {
//                            name = string.Format("{0} [{1}]", name, deviceType.DisplayName);
//                        }
//                    }

//                    if (variant == 2)
//                    {
//                        string dtDisplayName = null;
//                        if (node.GetGuidTag("deviceTypeId") != null)
//                        {
//                            dtDisplayName = GetFullName(cache, node.GetGuidTag("deviceTypeId").Value, user, 0);
//                        }
//                        string areaName = string.Empty;
//                        var rel = cache.Get<Relation>(r => r.EndNodeId == node.Id, user).FirstOrDefault();
//                        if (rel != null)
//                        {
//                            var area = cache.ById(rel.StartNodeId, user) as Node;
//                            if (area != null)
//                                areaName = area.GetStringTag("name");
//                            name = areaName;
//                        }
//                        string additionalText = string.Empty;
//                        if (!string.IsNullOrEmpty(dtDisplayName))
//                        {
//                            additionalText = dtDisplayName + ":";
//                        }
//                        if (node.GetIntTag("channel").HasValue)
//                        {
//                            additionalText += node.GetIntTag("channel");
//                        }
//                        if (!string.IsNullOrEmpty(additionalText))
//                        {
//                            name = string.Format("{0} [{1}]", name, additionalText);
//                        }
//                    }

//                    return name;
//                }

//                if (node.Type == "MatrixConnection")
//                {
//                    return string.Format("{0}", node.GetStringTag("imei"));
//                }
//                if (node.Type == "LanConnection")
//                {
//                    return string.Format("{0}:{1}", node.GetStringTag("ip"), node.GetStringTag("port"));
//                }
//                if (node.Type == "CsdConnection")
//                {
//                    return string.Format("{0}", node.GetStringTag("phone"));
//                }
//                if (node.Type == "ZigbeeConnection")
//                {
//                    return string.Format("{0}", node.GetStringTag("mac"));
//                }
//                if (node.Type == "ComConnection")
//                {
//                    return string.Format("{0}", node.GetStringTag("port"));
//                }
//            }
//            return entity.ToString();
//        }


//        public static ChangeLog CreateChangeLog(object changedModel, object oldModel, SessionUser user, ICache cache)
//        {
//            if (changedModel == null || user == null) return null;

//            string message = GetMessage(changedModel, oldModel, user, cache);
//            if (string.IsNullOrEmpty(message)) return null;

//            Guid modelId = default(Guid);
//            if (changedModel is Tagged)
//            {
//                modelId = (changedModel as Tagged).Id;
//            }

//            return new ChangeLog
//            {
//                Id = Guid.NewGuid(),
//                Message = message,
//                ObjectId = modelId,
//                ObjectName = cache.GetFullName(modelId, user),
//                RaiseTime = DateTime.Now,
//                UserId = user.User.Id,
//                UserName = user.User.ToString()
//            };
//        }

//        private static string GetMessage(object changedModel, object oldModel, SessionUser user, ICache cache)
//        {
//            string message = string.Empty;
//            if (oldModel == null)
//            {
//                message = "Объект добавлен";
//            }
//            else if (changedModel == null)
//            {
//                message = "Объект удален";
//            }
//            else
//            {
//                var changes = GetChanges(changedModel, oldModel);
//                if (!changes.Any()) return null; //объект не изменился

//                for (int i = 0; i < changes.Count; i++)
//                {
//                    string ret = string.Empty;
//                    if (i != changes.Count - 1)
//                        ret = "\r\n";
//                    message += string.Format("{0}{1}", GetMessage(changes[i], user, cache), ret);
//                }
//            }
//            return message;
//        }
//        private static string GetMessage(ChangeItem item, SessionUser user, ICache cache, string externalName = null)
//        {
//            if (item == null) return null;

//            if (item.ChangeType == ChangeType.Edited)
//            {
//                object newValue = item.NewValue;
//                object oldValue = item.OldValue;

//                if (newValue is Guid)
//                {
//                    newValue = cache.ById((Guid)newValue, user) ?? newValue;
//                }

//                if (oldValue is Guid)
//                {
//                    oldValue = cache.ById((Guid)oldValue, user) ?? oldValue;
//                }
//                if (!string.IsNullOrEmpty(externalName))
//                {
//                    externalName = externalName + ".";
//                }

//                return string.Format(@"Отредактировано поле ""{3}{0}"". Старое значение ""{1}"", новое значение ""{2}""",
//                                     item.PropertyName, oldValue, newValue, externalName);
//            }
//            if (item.ChangeType == ChangeType.ItemAdded)
//            {
//                string typeName;

//                DisplayTypeAttribute displayAttribute = null;
//                if (item.Type != null)
//                {
//                    displayAttribute = item.Type.GetCustomAttributes(typeof(DisplayTypeAttribute), true).FirstOrDefault() as DisplayTypeAttribute;
//                }
//                typeName = displayAttribute != null ? displayAttribute.Name : item.Type.Name;
//                typeName = typeName.First().ToString().ToLower() + String.Join("", typeName.Skip(1));//сделаем первую букву маленькой

//                if (!string.IsNullOrEmpty(externalName))
//                {
//                    externalName = " в " + externalName;
//                }

//                return string.Format(@"Добавлен новый {0} {1}{2}", typeName, item.NewValue, externalName);
//            }
//            if (item.ChangeType == ChangeType.ItemRemoved)
//            {
//                string typeName;
//                object[] displayAttributes = new object[] { };
//                if (item.Type != null)
//                {
//                    displayAttributes = item.Type.GetCustomAttributes(typeof(DisplayTypeAttribute), true);
//                }
//                if (displayAttributes.Any())
//                {
//                    typeName = (displayAttributes.FirstOrDefault() as DisplayTypeAttribute).Name;
//                }
//                else
//                {
//                    typeName = item.Type.Name;
//                }
//                typeName = typeName.First().ToString().ToLower() + String.Join("", typeName.Skip(1));//сделаем первую букву маленькой
//                if (!string.IsNullOrEmpty(externalName))
//                {
//                    externalName = " в " + externalName;
//                }

//                return string.Format(@"Удален {0} {1}{2}", typeName, item.OldValue, externalName);
//            }
//            if (item.ChangeType == ChangeType.ItemEdited)
//            {
//                if (!string.IsNullOrEmpty(externalName))
//                {
//                    externalName = externalName + ".";
//                }
//                string result = string.Empty;
//                string internalName = string.Format("{0}{1}", externalName, item.NewValue);
//                if (item.InnerChanges != null)
//                {
//                    for (int i = 0; i < item.InnerChanges.Count; i++)
//                    {
//                        string ret = string.Empty;
//                        if (i != item.InnerChanges.Count - 1)
//                            ret = "\r\n";

//                        result += string.Format("{0}{1}", GetMessage(item.InnerChanges[i], user, cache, internalName), ret);
//                    }
//                }
//                return result;
//            }
//            if (item.ChangeType == ChangeType.InnerChange)
//            {
//                if (!string.IsNullOrEmpty(externalName))
//                {
//                    externalName = externalName + ".";
//                }
//                string result = string.Empty;
//                string internalName = string.Format("{0}{1}", externalName, item.PropertyName);
//                if (item.InnerChanges != null)
//                {
//                    for (int i = 0; i < item.InnerChanges.Count; i++)
//                    {
//                        string ret = string.Empty;
//                        if (i != item.InnerChanges.Count - 1)
//                            ret = "\r\n";

//                        result += string.Format("{0}{1}", GetMessage(item.InnerChanges[i], user, cache, internalName), ret);
//                    }
//                }
//                return result;
//            }

//            return null;
//        }

//        private static List<ChangeItem> GetChanges(object changedModel, object oldModel)
//        {
//            var changes = new List<ChangeItem>();
//            if (changedModel == null && oldModel == null) return changes;
//            if (changedModel == null) throw new ArgumentNullException("changedModel");
//            if (oldModel == null) throw new ArgumentNullException("oldModel");

//            Type type = changedModel.GetType();
//            if (type != oldModel.GetType())
//            {
//                throw new ArgumentException(
//                    string.Format("Несовпадение типа измененной и сохраненной моделей. Тип измененной модели - {0}, сохраненной - {1}", type.Name, oldModel.GetType().Name));
//            }

//            PropertyInfo[] properties = type.GetProperties();
//            foreach (var propertyInfo in properties)
//            {
//                if (propertyInfo.PropertyType.IsSubclassOf(typeof(AggregationRoot))) continue;

//                object newValue = propertyInfo.GetValue(changedModel, null);
//                object oldValue = propertyInfo.GetValue(oldModel, null);

//                if ((newValue is IEnumerable && !(newValue is string)) || (oldValue is IEnumerable && !(oldValue is string)))
//                {
//                    var newCollection = new List<object>((newValue as IEnumerable ?? new List<object>()).Cast<object>());
//                    var oldCollection = new List<object>((oldValue as IEnumerable ?? new List<object>()).Cast<object>());

//                    foreach (var item in newCollection)
//                    {
//                        if (item is Tag)
//                        {
//                            var tag = item as Tag;
//                            var oldTag = oldCollection.FirstOrDefault(t => t is Tag && (t as Tag).Id == tag.Id) as Tag;
//                            if (oldTag == null)
//                            {
//                                changes.Add(new ChangeItem(typeof(string))
//                                                {
//                                                    ChangeType = ChangeType.Edited,
//                                                    PropertyName = tag.Name,
//                                                    NewValue = changedModel.ToString(),
//                                                    OldValue = null
//                                                });
//                                continue;
//                            }
//                            if (tag.Value != oldTag.Value)
//                            {
//                                changes.Add(new ChangeItem(typeof(string))
//                                                {
//                                                    ChangeType = ChangeType.Edited,
//                                                    PropertyName = tag.Name,
//                                                    NewValue = changedModel.ToString(),
//                                                    OldValue = oldTag.Value,
//                                                });
//                            }
//                            oldCollection.Remove(oldTag);
//                            continue;
//                        }

//                        if (item is Tagged)
//                        {
//                            var tagged = item as Tagged;
//                            var oldTagged = oldCollection.FirstOrDefault(t => t is Tagged && (t as Tagged).Id == tagged.Id);
//                            if (oldTagged == null)
//                            {
//                                changes.Add(new ChangeItem(typeof(string))
//                                {
//                                    ChangeType = ChangeType.ItemAdded,
//                                    PropertyName = GetPropertyDisplay(propertyInfo),
//                                    InnerChanges = new List<ChangeItem>{
//                                        new ChangeItem(item.GetType()) {
//                                            ChangeType = ChangeType.ItemAdded,											
//                                            NewValue = item.ToString()
//                                        }
//                                    }
//                                });
//                                continue;
//                            }
//                            List<ChangeItem> itemChanges = GetChanges(tagged, oldTagged);
//                            if (itemChanges.Any())
//                            {
//                                changes.Add(new ChangeItem(typeof(string))
//                                {
//                                    ChangeType = ChangeType.ItemEdited,
//                                    PropertyName = GetPropertyDisplay(propertyInfo),
//                                    NewValue = tagged.ToString(),
//                                    InnerChanges = itemChanges
//                                });
//                            }

//                            oldCollection.Remove(oldTagged);
//                        }
//                    }
//                    foreach (var oldTagged in oldCollection)
//                    {
//                        if (oldTagged is Tag)
//                        {
//                            var tag = oldTagged as Tag;

//                            changes.Add(new ChangeItem(typeof(string))
//                                            {
//                                                ChangeType = ChangeType.Edited,
//                                                PropertyName = tag.Name,
//                                                NewValue = changedModel.ToString()
//                                            });
//                            continue;

//                        }

//                        changes.Add(new ChangeItem(oldTagged.GetType())
//                        {
//                            ChangeType = ChangeType.ItemRemoved,
//                            PropertyName = GetPropertyDisplay(propertyInfo),
//                            OldValue = oldTagged.ToString()
//                        });
//                    }
//                    continue;
//                }

//                if (newValue == null && oldValue == null) continue;
//                if (newValue != null && newValue.Equals(oldValue)) continue;//значения равны

//                var propertyType = propertyInfo.PropertyType;
//                if (!propertyType.IsPrimitive && !propertyType.IsEnum && propertyType != typeof(string) && propertyType != typeof(Guid))//какой то сложный объект
//                {
//                    if (newValue != null && oldValue != null)
//                    {
//                        List<ChangeItem> innerChanges = GetChanges(newValue, oldValue);
//                        if (innerChanges.Any())
//                        {
//                            changes.Add(new ChangeItem(propertyInfo.PropertyType)
//                                            {
//                                                ChangeType = ChangeType.InnerChange,
//                                                PropertyName = GetPropertyDisplay(propertyInfo),
//                                                InnerChanges = innerChanges
//                                            });
//                        }
//                        continue;
//                    }
//                }

//                var newValueString = newValue != null ? newValue.ToString() : "null";
//                var oldValueString = oldValue != null ? oldValue.ToString() : "null";
//                changes.Add(new ChangeItem(propertyInfo.PropertyType)
//                                {
//                                    ChangeType = ChangeType.Edited,
//                                    PropertyName = GetPropertyDisplay(propertyInfo),
//                                    NewValue = newValueString,
//                                    OldValue = oldValueString,
//                                });
//            }
//            return changes;
//        }

//        /// <summary>
//        /// todo разобраться позже
//        /// </summary>
//        /// <param name="original"></param>
//        /// <param name="other"></param>
//        //private void GetChanges(IEntity original, IEntity other)
//        //{
//        //    var entities = new Queue<IEntity>();
//        //    entities.Enqueue(original);

//        //    while (entities.Count > 0)
//        //    {
//        //        var properties = original.GetType().GetProperties();
//        //        foreach (var property in properties)
//        //        {
//        //            if (property.PropertyType.IsSubclassOf(typeof(IEntity)))
//        //            {
//        //                entities.Enqueue((IEntity)property.GetValue(original, null));
//        //            }
//        //            else if (property is IEnumerable)
//        //            {

//        //            }
//        //            else
//        //            {

//        //            }
//        //        }
//        //    }
//        //}

//        private static string GetPropertyDisplay(PropertyInfo propertyInfo)
//        {
//            var propertyDisplay = propertyInfo.Name;
//            object[] displayAttributes = propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), true);
//            if (displayAttributes.Any())
//            {
//                var displayAttribute = displayAttributes.FirstOrDefault() as DisplayAttribute;
//                if (displayAttribute != null)
//                    propertyDisplay = displayAttribute.Name;
//            }

//            return propertyDisplay;
//        }
//        public static string GetTypeName(Type t)
//        {
//            var displayAttribute = t.GetCustomAttributes(typeof(DisplayTypeAttribute), true).FirstOrDefault() as DisplayTypeAttribute;
//            var typeName = displayAttribute != null ? displayAttribute.Name : t.Name;
//            return typeName;
//        }

//        private enum ChangeType
//        {
//            Edited,
//            ItemAdded,
//            ItemRemoved,
//            ItemEdited,
//            InnerChange
//        }
//        private class ChangeItem
//        {
//            public ChangeItem(Type type)
//            {
//                Type = type;
//            }

//            /// <summary>
//            /// Нужен в случае, когда в коллекцию добавили или удалили элемент
//            /// </summary>
//            public Type Type { get; set; }
//            public string PropertyName { get; set; }
//            public ChangeType ChangeType { get; set; }
//            public List<ChangeItem> InnerChanges { get; set; }
//            public string OldValue { get; set; }
//            public string NewValue { get; set; }
//        }
//    }
//}
