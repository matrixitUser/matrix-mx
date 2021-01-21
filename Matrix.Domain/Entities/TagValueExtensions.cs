using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// синтаксический сахар
    /// облегчает работу с тегами, находя нужный и преобразуя его в нужный тип
    /// </summary>
    public static class TagValueExtensions
    {
        /// <summary>
        /// находит и преобразует тег в double
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static double? GetDoubleTag(this Entity cached, string tagName)
        {
            double? value = null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            double parsedValue = 0.0;
            if (double.TryParse(tag.Value, out parsedValue))
                value = parsedValue;
            return value;
        }

        /// <summary>
        /// находит и преобразует тег в int
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static int? GetIntTag(this Entity cached, string tagName)
        {
            int? value = null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            int parsedValue = 0;
            if (int.TryParse(tag.Value, out parsedValue))
                value = parsedValue;
            return value;
        }

        /// <summary>
        /// находит и преобразует тег в int
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static bool? GetBoolTag(this Entity cached, string tagName)
        {
            bool? value = null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            bool parsedValue = false;
            if (bool.TryParse(tag.Value, out parsedValue))
                value = parsedValue;
            return value;
        }

        /// <summary>
        /// находит и преобразует тег в bool
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static DateTime? GetDateTimeTag(this Entity cached, string tagName)
        {
            DateTime? value = null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            DateTime parsedValue = DateTime.MinValue;
            if (DateTime.TryParse(tag.Value, out parsedValue))
                value = parsedValue;
            return value;
        }

        public static void SetTag(this Entity cached, string tagName, object value, bool isSpecial = false)
        {
            if (cached.Tags == null)
            {
                cached.Tags = new List<Tag>();
            }

            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tag == null && value != null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    TaggedId = cached.Id,
                    Name = tagName,
                    Value = value.ToString(),
                    IsSpecial = isSpecial
                };
                cached.Tags.Add(tag);
            }
            else
            {
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                {
                    cached.Tags.Remove(tag);
                }
                else
                {
                    tag.Value = value.ToString();
                }
            }
        }

        /// <summary>
        /// находит строковое значение тега
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static string GetStringTag(this Entity cached, string tagName)
        {
            if (cached == null) return null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            return tag.Value;
        }

        /// <summary>
        /// находит GUID значение тега
        /// </summary>
        /// <param name="cached"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static Guid? GetGuidTag(this Entity cached, string tagName)
        {
            if (cached == null) return null;
            if (cached.Tags == null) return null;
            var tag = cached.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag == null) return null;
            Guid guid = Guid.Empty;
            Guid.TryParse(tag.Value, out guid);
            return guid;
        }

        /// <summary>
        /// возвращает тег-коллекцию как коллекцию гуидов
        /// </summary>
        /// <param name="tagged"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static IEnumerable<Guid> GetListOfGuid(this Entity tagged, string tagName)
        {
            if (tagged == null) return null;
            if (tagged.Tags == null) return null;
            var tags = tagged.Tags.Where(t => t.Name == tagName);
            if (tags == null) return null;
            var guids = new List<Guid>();
            foreach (var tag in tags)
            {
                Guid guid = Guid.Empty;
                if (Guid.TryParse(tag.Value, out guid))
                    guids.Add(guid);
            }
            return guids;
        }

        /// <summary>
        /// возвращает тег-коллекцию как коллекцию строк
        /// </summary>
        /// <param name="tagged"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetListOfString(this Entity tagged, string tagName)
        {
            if (tagged == null) return null;
            if (tagged.Tags == null) return null;
            var tags = tagged.Tags.Where(t => t.Name == tagName);
            if (tags == null) return null;
            return tags.Select(t => t.Value).ToList();
        }

        /// <summary>
        /// добавляет значение в тег-коллекцию
        /// </summary>
        /// <param name="tagged"></param>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        public static void AddListItem(this Entity tagged, string tagName, object value)
        {
            if (tagged.Tags == null)
            {
                tagged.Tags = new List<Tag>();
            }

            var stringValue = value.ToString();

            var oldTag = tagged.Tags.FirstOrDefault(t => t.Name == tagName && t.Value == stringValue);

            if (oldTag != null)
            {
                if (value == null || string.IsNullOrEmpty(stringValue))
                {
                    tagged.Tags.Remove(oldTag);
                }
                else
                {
                    oldTag.Value = stringValue;
                }
            }
            else
            {
                var tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    IsSpecial = false,
                    Name = tagName,
                    TaggedId = tagged.Id,
                    Value = stringValue
                };
                tagged.Tags.Add(tag);
            }
        }

        /// <summary>
        /// удаляет елемент из тега-коллекции
        /// </summary>
        /// <param name="tagged"></param>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        public static void RemoveListItem(this Entity tagged, string tagName, object value)
        {
            if (tagged.Tags == null)
            {
                tagged.Tags = new List<Tag>();
            }

            var stringValue = value.ToString();

            var oldTag = tagged.Tags.FirstOrDefault(t => t.Name == tagName && t.Value == stringValue);

            if (oldTag != null)
            {
                tagged.Tags.Remove(oldTag);
            }
        }

        /// <summary>
        /// очистка тега-коллекции
        /// </summary>
        /// <param name="tagged"></param>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        public static void ClearList(this Entity tagged, string tagName)
        {
            if (tagged.Tags == null)
            {
                tagged.Tags = new List<Tag>();
            }

            var removedTags = tagged.Tags.Where(t => t.Name == tagName).ToList();

            foreach (var removedTag in removedTags)
            {
                tagged.Tags.Remove(removedTag);
            }
        }
    }
}
