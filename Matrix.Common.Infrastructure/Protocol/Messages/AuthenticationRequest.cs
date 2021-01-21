//using System;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос аутентификации
//    /// не проверяется фильтрами
//    /// </summary>
//    public class AuthenticationRequest : Message
//    {
//        public string Login { get; private set; }
//        public string Password { get; private set; }

//        public AuthenticationRequest(Guid id, string login, string password)
//            : base(id)
//        {
//            Login = login;
//            Password = password;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос на аутентификацию от пользователя {0}", Login);
//        }
//    }
//}
