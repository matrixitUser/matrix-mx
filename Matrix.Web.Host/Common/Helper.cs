using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Common
{
    static class Helper
    {
        public static dynamic Wrap(Entity node)
        {
            dynamic tube = new JObject();
            tube.id = node.Id;

            if (node is Node)
            {
                tube.type = (node as Node).Type;
                try
                {
                    //if ((node as Node).Type == "Tube" && node.GetGuidTag("deviceTypeId").HasValue)
                    //{
                    //    var dt = (DeviceType)Cache.Instance.GetById(node.GetGuidTag("deviceTypeId").Value);
                    //    tube.deviceType = dt.DisplayName;
                    //}
                }
                catch (Exception exc)
                {

                }
            }

            if (node is Relation)
            {
                tube.start = (node as Relation).StartNodeId;
                tube.end = (node as Relation).EndNodeId;
                tube.type = "Relation";
            }

            foreach (var tag in node.Tags)
            {
                if (tube[tag.Name] != null)
                {
                    if (tube[tag.Name].Type == JTokenType.Array)
                    {
                        tube[tag.Name].Add(tag.Value);
                    }
                    else
                    {

                    }
                }
                else
                {
                    tube.Add(tag.Name, JToken.FromObject(tag.Value));
                }
            }
            return tube;
        }
    }
}
