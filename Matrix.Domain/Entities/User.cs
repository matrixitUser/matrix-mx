using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class User : Entity
    {
       
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public bool IsDomainUser { get; set; }
        [DataMember]
        public string Surname { get; set; }
        [DataMember]
        public string Patronymic { get; set; }
        [DataMember]
        public Guid GroupId { get; set; }
        [DataMember]
        public string EMail { get; set; }
        [DataMember]
        public bool IsAdmin { get; set; }

        public string ShortName
        {
            get
            {
                if (string.IsNullOrEmpty(Surname))
                    return Name ?? string.Empty;

                string result = Surname;

                if (!string.IsNullOrEmpty(Name))
                {
                    result = result + string.Format(" {0}.", Name[0]);
                    if (!string.IsNullOrEmpty(Patronymic))
                    {
                        result = result + string.Format(" {0}.", Patronymic[0]);
                    }
                }
                return result;
            }
        }
        public string FullName
        {
            get
            {
                return string.Format("{0} {1} {2}", Surname, Name, Patronymic);
            }
        }
        public override string ToString()
        {
            return ShortName;
        }
    }
}
