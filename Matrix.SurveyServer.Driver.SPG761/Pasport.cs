using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {

        private dynamic GetPassport(byte dad, byte sad)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;

            //проверка адреса
            dynamic parameter = GetParameters(dad, sad, new Dictionary<string, string> { { "003", "0" } }, false);
            if (!parameter.success)
                return parameter;

            var address = int.Parse(parameter.categories[0][0].Substring(5, 2));
            answer.needDad = dad != address;

            //var categories = new Dictionary<string, string>()
            //{
            //    { "003", "0" },
            //    { "099", "0" }                            
            //};

            //dynamic passport = GetParameters(dad, sad, categories, answer.needDad);
            //if (!passport.success)
            //    return passport;



            if (answer.needDad)
            {
                parameter = GetParameters(dad, sad, new Dictionary<string, string> { { "003", "0" } }, answer.needDad);
                if (!parameter.success)
                    return parameter;
            }

            dynamic passport = GetParameters(dad, sad, new Dictionary<string, string> { { "099", "0" } }, answer.needDad);
            if (!passport.success)
                return passport;

            answer.model = passport.categories[0][0];
            byte n = 0;
            byte.TryParse(answer.model[4].ToString(), out n);
            answer.n = n;

            return answer;
        }
    }
}
