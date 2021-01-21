using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.RecordsSaver
{
    public class SaveRulesSection : ConfigurationSection
    {
        const string SECTION_NAME = "save-rules";

        const string PROPERTY_RULES = "rules";
        [ConfigurationProperty(PROPERTY_RULES, IsRequired = true, IsDefaultCollection = true)]
        public RulesCollection Rules
        {
            get
            {
                return (RulesCollection)this[PROPERTY_RULES];
            }
            set
            {
                this[PROPERTY_RULES] = value;
            }
        }

        private static SaveRulesSection instance;
        public static SaveRulesSection Instance
        {
            get
            {
                if (instance == null)
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    instance = (SaveRulesSection)config.GetSection(SECTION_NAME);
                    if (instance == null)
                    {
                        instance = new SaveRulesSection();
                        config.Sections.Add(SECTION_NAME, instance);
                    }
                }
                return instance;
            }
        }
    }

    public class RuleElement : ConfigurationElement
    {
        const string PROPERTY_TYPE = "type";
        [ConfigurationProperty(PROPERTY_TYPE, DefaultValue = "правило", IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this[PROPERTY_TYPE];
            }
        }

        const string PROPERTY_ADDITIONAL_FIELDS = "additional-fields";
        [ConfigurationProperty(PROPERTY_ADDITIONAL_FIELDS, DefaultValue = "", IsRequired = true)]
        public string Fields
        {
            get
            {
                return (string)this[PROPERTY_ADDITIONAL_FIELDS];
            }
        }

        const string PROPERTY_INDEX_FIELDS = "index-fields";
        [ConfigurationProperty(PROPERTY_INDEX_FIELDS, IsRequired = true)]
        public string IndexFields
        {
            get
            {
                return (string)this[PROPERTY_INDEX_FIELDS];
            }
        }

        public string[] GetIndexFields()
        {
            return IndexFields.Split(';');
        }

        public Dictionary<string, string> GetFields()
        {
            var fields = Fields.Split(';');
            var dict = new Dictionary<string, string>();
            dict.Add("id", "uniqueidentifier");
            dict.Add("objectId", "uniqueidentifier");
            dict.Add("type", "nvarchar(100)");
            dict.Add("date", "datetime2(7)");

            foreach (var field in fields)
            {
                var pair = field.Split(':');
                var name = "";
                if (pair.Length >= 1)
                {
                    name = pair[0];
                }
                var type = "";
                if (pair.Length >= 2)
                {
                    type = pair[1];
                }
                else
                {
                    type = "nvarchar(max)";
                }
                if (!dict.ContainsKey(name))
                {
                    dict.Add(name, type);
                }
            }

            return dict;
        }
    }

    public static class HelperExtensions
    {
        public static IEnumerable<string> ToStringArray(this FieldsCollection col)
        {
            foreach (FieldElement element in col)
            {
                yield return element.Name;
            }
        }
    }

    public class RulesCollection : ConfigurationElementCollection
    {

        protected override string ElementName
        {
            get
            {
                return "rule";
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RuleElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as RuleElement).Type;
        }
    }

    public class FieldsCollection : ConfigurationElementCollection
    {

        protected override ConfigurationElement CreateNewElement()
        {
            return new FieldElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as FieldElement).Name;
        }
    }

    public class FieldElement : ConfigurationElement
    {
        const string PROPERTY_NAME = "name";
        [ConfigurationProperty(PROPERTY_NAME, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this[PROPERTY_NAME];
            }
        }
    }
}
