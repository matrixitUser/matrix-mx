using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Matrix.Domain.Entities
{
    public static class EntityExtensions
    {
        public static dynamic ToJSONDynamic(this Entity entity)
        {			
            dynamic json = new JObject();
            json.id = entity.Id;

            if (entity is Node)
            {
                json.@class = "node";
                json.type = (entity as Node).Type;
            }

            if (entity is Relation)
            {
                var relation = entity as Relation;
                json.@class = "relation";
                json.start = relation.StartNodeId;
                json.end = relation.EndNodeId;
                json.type = "Relation";
            }

            if (entity is DeviceType)
            {
                var driver = entity as DeviceType;
                json.@class = "driver";
                json.driver = driver.Driver;
                json.name = driver.DisplayName;
            }

            if (entity is GsmModem)
            {
				var modem = entity as GsmModem;
                json.@class = "modem";
                json.baudRate = modem.BaudRate;
                json.comPort = modem.ComPort;
                json.csdPortId = modem.CsdPortId;
            }

            if (entity is Report)
            {
                var modem = entity as Report;
                json.@class = "report";
                json.name = modem.Name;
                json.template = modem.Template;
            }

            if (entity is Group)
            {
                var group = entity as Group;
                json.@class = "group";
                json.name = group.Name;
                json.parentId = group.ParentId;
            }

            if (entity is User)
            {
                var user = entity as User;
                json.@class = "user";
                json.id = user.Id;
                json.name = user.Name;
                json.groupId = user.GroupId;
                json.login = user.Login;
                json.password = user.Password;
                json.patronymic = user.Patronymic;
                json.surname = user.Surname;
                json.mail = user.EMail;
                json.isAdmin = user.IsAdmin;
            }

            foreach (var tag in entity.Tags)
            {
                if (json[tag.Name] != null)
                {
                    if (json[tag.Name].Type == JTokenType.Array)
                    {
                        json[tag.Name].Add(tag.Value);
                    }
                    else
                    {

                    }
                }
                else
                {
                    json.Add(tag.Name, JToken.FromObject(tag.Value));
                }
            }
            return json;
        }

        private static string ToCamel(string source)
        {
            return char.ToLower(source[0]) + source.Substring(1);
        }

        public static dynamic ToJSONDynamic(this DataRecord record)
        {
            dynamic json = new JObject();

            foreach (var property in typeof(DataRecord).GetProperties())
            {
				if (property.GetValue(record,null) != null)
					json.Add(ToCamel(property.Name), JToken.FromObject(property.GetValue(record,null)));
            }
            return json;
        }


        public static DataRecord ToRecord(dynamic json)
        {
            DataRecord record = new DataRecord();

            var djson = json as IDictionary<string, object>;


            if (djson.ContainsKey("id")) record.Id = Guid.Parse((string)json.id);

            if (djson.ContainsKey("d1")) record.D1 = (double)json.d1;
            if (djson.ContainsKey("d2")) record.D2 = (double)json.d2;
            if (djson.ContainsKey("d3")) record.D3 = (double)json.d3;
            if (djson.ContainsKey("date")) record.Date = json.date;
            if (djson.ContainsKey("dt1")) record.Dt1 = json.dt1;
            if (djson.ContainsKey("dt2")) record.Dt2 = json.dt2;
            if (djson.ContainsKey("dt3")) record.Dt3 = json.dt3;
            if (djson.ContainsKey("g1")) record.G1 = Guid.Parse(json.g1);
            if (djson.ContainsKey("g2")) record.G2 = json.g2;
            if (djson.ContainsKey("g3")) record.G3 = json.g3;
            if (djson.ContainsKey("i1")) record.I1 = (int)json.i1;
            if (djson.ContainsKey("i2")) record.I2 = (int)json.i2;
            if (djson.ContainsKey("i3")) record.I3 = (int)json.i3;
            if (djson.ContainsKey("objectId")) record.ObjectId = Guid.Parse((string)json.objectId);
            if (djson.ContainsKey("s1")) record.S1 = json.s1;
            if (djson.ContainsKey("s2")) record.S2 = json.s2;
            if (djson.ContainsKey("s3")) record.S3 = json.s3;
            if (djson.ContainsKey("type")) record.Type = json.type;

            return record;
        }

        public static Entity ToEntity(dynamic json)
        {
            Entity entity = null;

            var exceptProperties = new List<string>();
            exceptProperties.Add("class");

            string @class = json.@class;
            switch (@class.ToLower())
            {
                case "driver":
                    entity = new DeviceType()
                    {
                        Id = json.id,
                        DisplayName = json.name,
                        Driver = Convert.FromBase64String((string)json.driver),
                        Name = json.name
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("name");
                    exceptProperties.Add("driver");
                    break;
                case "node":
                    entity = new Node()
                    {
                        Id = json.id,
                        Type = json.type
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("type");
                    break;
                case "relation":
                    entity = new Relation()
                    {
                        Id = json.id,
                        StartNodeId = json.start,
                        EndNodeId = json.end
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("type");
                    exceptProperties.Add("start");
                    exceptProperties.Add("end");
                    break;
                case "modem":

                    entity = new GsmModem()
                    {
                        Id = json.id,
                        BaudRate = json.baudRate,
                        ComPort = json.comPort,
                        CsdPortId = json.csdPortId
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("baudRate");
                    exceptProperties.Add("comPort");
                    exceptProperties.Add("csdPortId");
                    break;
                case "report":
                    entity = new Report()
                    {
                        Id = json.id,
                        Name = json.name,
                        Template = json.template
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("name");
                    exceptProperties.Add("template");
                    break;
                case "group":
                    entity = new Group()
                    {
                        Id = json.id,
                        Name = json.name,
                        ParentId = json.parentId
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("name");
                    exceptProperties.Add("parentId");
                    break;
                case "user":
                    entity = new User()
                    {
                        Id = json.id,
                        Name = json.name,
                        GroupId = json.groupId,
                        Login = json.login,
                        Password = json.password,
                        Patronymic = json.patronymic,
                        Surname = json.surname,
                        EMail = json.mail,
                        IsAdmin = json.isAdmin
                    };
                    exceptProperties.Add("id");
                    exceptProperties.Add("name");
                    exceptProperties.Add("groupId");
                    exceptProperties.Add("login");
                    exceptProperties.Add("password");
                    exceptProperties.Add("patronymic");
                    exceptProperties.Add("surname");
                    exceptProperties.Add("mail");
                    exceptProperties.Add("isAdmin");
                    break;
            }

            if (entity == null) return null;

            foreach (JProperty property in json.Properties())
            {
                if (exceptProperties.Contains(property.Name)) continue;
                property.Value<object>();
                entity.SetTag(property.Name, property.Value.ToString());
            }

            return entity;
        }
    }
}
