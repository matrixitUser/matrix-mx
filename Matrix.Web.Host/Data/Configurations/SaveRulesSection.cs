using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
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
        const string PROPERTY_NAME = "name";
        [ConfigurationProperty(PROPERTY_NAME, DefaultValue = "правило", IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this[PROPERTY_NAME];
            }
        }

        const string PROPERTY_FORMAT = "format";
        [ConfigurationProperty(PROPERTY_FORMAT, DefaultValue = "", IsRequired = true)]
        public string Format
        {
            get
            {
                return (string)this[PROPERTY_FORMAT];
            }
        }

        const string PROPERTY_FORMAT_FIELDS = "format-fields";
        [ConfigurationProperty(PROPERTY_FORMAT_FIELDS, IsRequired = true, IsDefaultCollection = true)]
        public FieldsCollection FormatFields
        {
            get
            {
                return (FieldsCollection)this[PROPERTY_FORMAT_FIELDS];
            }
        }

        const string PROPERTY_UNIQUE_FIELDS = "unique-fields";
        [ConfigurationProperty(PROPERTY_UNIQUE_FIELDS, IsRequired = true, IsDefaultCollection = true)]
        public FieldsCollection UniqueFields
        {
            get
            {
                return (FieldsCollection)this[PROPERTY_UNIQUE_FIELDS];
            }
        }

        const string PROPERTY_TYPES = "types";
        [ConfigurationProperty(PROPERTY_TYPES, IsRequired = true, IsDefaultCollection = true)]
        public FieldsCollection Types
        {
            get
            {
                return (FieldsCollection)this[PROPERTY_TYPES];
            }
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
            return (element as RuleElement).Name;
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
