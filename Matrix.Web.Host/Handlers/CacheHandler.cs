using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Handlers
{
    class CacheHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("cache");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse(session.userId.ToString());

            if (what == "cache-clear")
            {
                Guid id = Guid.Parse(message.body.id.ToString());
                dynamic cache = new ExpandoObject();
                Data.CacheRepository.Instance.SaveCache(id, cache);
            }
            return Helper.BuildMessage(what);
        }
    }
}
