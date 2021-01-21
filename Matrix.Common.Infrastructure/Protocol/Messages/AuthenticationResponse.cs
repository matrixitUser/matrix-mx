//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// не обрабатывается фильтрами
//    /// </summary>
//    public class AuthenticationResponse : Message
//    {
//        public AuthenticationResponse(Guid id, User user, Group group)
//            : base(id)
//        {
//            User = user;
//            Group = group;
//        }

//        public User User { get; private set; }
//        public Group Group { get; private set; }

//        public override string ToString()
//        {
//            return string.Format("ответ на запрос аутентификации, пользователь {0}", User);
//        }
//    }
//}
