using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Neo4jClient;
using Neo4jClient.Cypher;
using Newtonsoft.Json;

namespace Matrix.Web.Host.Handlers
{
    static class Helper
    {
        public static dynamic BuildMessage(string what)
        {
            dynamic success = new ExpandoObject();
            success.head = new ExpandoObject();
            success.head.what = what;
            success.body = new ExpandoObject();
            return success;
        }
    }
}
