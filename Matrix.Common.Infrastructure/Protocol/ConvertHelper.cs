//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol
//{
//    public static class ConvertHelper
//    {
//        public static byte[] SerializeMessage(BaseMessage message)
//        {
//            try
//            {
//                return SerializeComplexObject(message);
//            }
//            catch (Exception e)
//            {
//                throw new Exception("Ошибка при сериализации объекта", e);
//            }
//        }
//        public static BaseMessage DeserializeMessage(byte[] data)
//        {
//            try
//            {
//                return DeserializeComplexObject(data.ToList()) as BaseMessage;
//            }
//            catch (Exception e)
//            {
//                throw new Exception("Ошибка при десериализации объекта", e);
//            }
//        }
//        private static byte[] GetBytes(object data)
//        {
//            var result = new List<byte>();

//            byte[] bytes = null;
//            if (data == null)
//            {
//                bytes = new byte[0];
//            }
//            else if (data is bool)
//            {
//                bytes = new[] { ((bool)data) ? (byte)1 : (byte)0 };
//            }
//            else if (data is int)
//            {
//                bytes = BitConverter.GetBytes((int)data);
//            }
//            else if (data is double)
//            {
//                bytes = BitConverter.GetBytes((double)data);
//            }
//            else if (data is string)
//            {
//                bytes = Encoding.UTF8.GetBytes((string)data);
//            }
//            else if (data is Guid)
//            {
//                bytes = Encoding.UTF8.GetBytes(data.ToString());
//            }
//            else if (data is DateTime)
//            {
//                long ticks = ((DateTime)data).ToBinary();
//                bytes = BitConverter.GetBytes(ticks);
//            }
//            else if (data is long)
//            {
//                bytes = BitConverter.GetBytes((long)data);
//            }
//            else if (data is Int16)
//            {
//                bytes = BitConverter.GetBytes((Int16)data);
//            }
//            else if (data is UInt16)
//            {
//                bytes = BitConverter.GetBytes((UInt16)data);
//            }
//            else if (data is UInt32)
//            {
//                bytes = BitConverter.GetBytes((UInt32)data);
//            }
//            else if (data is UInt64)
//            {
//                bytes = BitConverter.GetBytes((UInt64)data);
//            }
//            else if (data is float)
//            {
//                bytes = BitConverter.GetBytes((float)data);
//            }
//            else if (data is byte)
//            {
//                bytes = new byte[(byte)data];
//            }
//            else if (data is char)
//            {
//                bytes = new byte[(char)data];
//            }
//            else if (data is IList)
//            {
//                var byteList = new List<byte>();
//                var ienumerable = (IEnumerable)data;
//                foreach (var obj in ienumerable)
//                {
//                    var b = GetBytes(obj);
//                    byteList.AddRange(b);
//                }
//                bytes = byteList.ToArray();
//            }
//            else if (data.GetType().IsClass)
//            {
//                //значение сложный объект - будем сериализовать его поля отдельно
//                bytes = SerializeComplexObject(data);
//            }

//            if (bytes != null)
//                AddBytes(result, bytes);

//            return result.ToArray();
//        }
//        private static byte[] SerializeComplexObject(object data)
//        {
//            if (data == null)
//            {
//                return new byte[0];
//            }

//            var result = new List<byte>();

//            Type type = data.GetType();
//            PropertyInfo[] properties = type.GetProperties();
//            foreach (PropertyInfo propertyInfo in properties)
//            {
//                if (!propertyInfo.CanRead || !propertyInfo.CanWrite) continue;
//                object[] attributes = propertyInfo.GetCustomAttributes(true);
//                if (attributes.Any(a => a is NonSerializedAttribute)) continue;
//                try
//                {
//                    object value = propertyInfo.GetValue(data, null);
//                    byte[] bytes = GetBytes(value);
//                    result.AddRange(bytes);
//                }
//                catch (Exception exception)
//                {
//                    var message = string.Format("Ошибка в {0}.{1}", type.Name, propertyInfo.Name);
//                    throw new Exception(message, exception);
//                }
//            }
//            return result.ToArray();
//        }
//        private static void AddBytes(List<byte> data, byte[] bytesToAdd)
//        {
//            var byteL = bytesToAdd.Length;
//            if (byteL >= byte.MaxValue)
//            {
//                if (byteL > UInt16.MaxValue)
//                {
//                    throw new ArgumentOutOfRangeException("bytesToAdd", "Длина поля при сериализации превосходит максимальную");
//                }
//                data.Add(byte.MaxValue);
//                var lengthBytes = BitConverter.GetBytes((UInt16)byteL);
//                data.AddRange(lengthBytes);
//            }
//            else
//            {
//                data.Add((byte)byteL);
//            }
//            data.AddRange(bytesToAdd);
//        }

//        private static object GetObject(List<byte> data, Type desireType)
//        {
//            object result = GetDefault(desireType);
//            var bytes = GetNextBytes(data);
//            if (bytes == null) return result;

//            if (desireType == typeof(int))
//            {
//                result = BitConverter.ToInt32(bytes, 0);
//            }
//            else if (desireType == typeof(double))
//            {
//                result = BitConverter.ToDouble(bytes, 0);
//            }
//            else if (desireType == typeof(string))
//            {
//                result = Encoding.UTF8.GetString(bytes);
//            }
//            else if (desireType == typeof(Guid))
//            {
//                var guidStr = Encoding.UTF8.GetString(bytes);
//                result = Guid.Parse(guidStr);
//            }
//            else if (desireType == typeof(DateTime))
//            {
//                long ticks = BitConverter.ToInt64(bytes, 0);
//                result = ticks == 0 ? DateTime.MinValue : DateTime.FromBinary(ticks);
//            }
//            else if (desireType == typeof(bool))
//            {
//                if (bytes[0] == 0) result = false;
//                else result = true;
//            }
//            else if (desireType == typeof(Int16))
//            {
//                result = BitConverter.ToInt16(bytes, 0);
//            }
//            else if (desireType == typeof(UInt16))
//            {
//                result = BitConverter.ToUInt16(bytes, 0);
//            }
//            else if (desireType == typeof(UInt32))
//            {
//                result = BitConverter.ToUInt32(bytes, 0);
//            }
//            else if (desireType == typeof(Int64))
//            {
//                result = BitConverter.ToInt64(bytes, 0);
//            }
//            else if (desireType == typeof(UInt64))
//            {
//                result = BitConverter.ToUInt64(bytes, 0);
//            }
//            else if (desireType == typeof(float))
//            {
//                result = BitConverter.ToSingle(bytes, 0);
//            }
//            else if (desireType == typeof(byte))
//            {
//                result = bytes[0];
//            }
//            else if (desireType == typeof(char))
//            {
//                result = bytes[0];
//            }
//            else if (desireType.IsGenericType && desireType.GetGenericTypeDefinition() == typeof(List<>))
//            {
//                var itemType = desireType.GetGenericArguments()[0];
//                var collection = Activator.CreateInstance(desireType) as IList;
//                if (collection != null)
//                {
//                    var byteList = bytes.ToList();
//                    while (byteList.Any())
//                    {
//                        var item = GetObject(byteList, itemType);
//                        collection.Add(item);
//                    }
//                }
//                result = collection;
//            }
//            else if (desireType.IsClass && !desireType.IsGenericType)
//            {
//                result = DeserializeComplexObject(bytes.ToList(), desireType);
//            }
//            return result;
//        }
//        private static object DeserializeComplexObject(List<byte> data)
//        {
//            if (data == null || data.Count == 0) return GetDefault(type);
//            object result = Activator.CreateInstance(type);


//            PropertyInfo[] properties = type.GetProperties();
//            foreach (PropertyInfo propertyInfo in properties)
//            {
//                if (!propertyInfo.CanRead || !propertyInfo.CanWrite) continue;
//                object[] attributes = propertyInfo.GetCustomAttributes(true);
//                if (attributes.Any(a => a is NonSerializedAttribute)) continue;
//                try
//                {
//                    var propertyType = propertyInfo.PropertyType;
//                    var value = GetObject(data, propertyType);
//                    propertyInfo.SetValue(result, value, null);
//                }
//                catch (Exception exception)
//                {
//                    var message = string.Format("Ошибка в {0}.{1}", type.Name, propertyInfo.Name);
//                    throw new Exception(message, exception);
//                }
//            }
//            return result;
//        }
//        /// <summary>
//        /// Достает следующие байты (в соответствии с длиной, указанной в начале данного массива),
//        /// а затем удаляет использованные байты из начала данного массива
//        /// </summary>
//        /// <param name="data"></param>
//        /// <returns></returns>
//        private static byte[] GetNextBytes(List<byte> data)
//        {
//            if (data == null)
//                throw new ArgumentNullException("data");
//            if (data.Count == 0)
//                throw new ArgumentOutOfRangeException("data", "Длина массива не может быть равно нулю");

//            int length = data[0];
//            data.RemoveAt(0);
//            if (length == byte.MaxValue)
//            {
//                length = BitConverter.ToUInt16(data.ToArray(), 0);
//                data.RemoveRange(0, 2);
//            }
//            if (length > data.Count)
//            {
//                throw new ArgumentOutOfRangeException("data", "Неожиданный конец архива");
//            }
//            byte[] result = data.Take(length).ToArray();
//            data.RemoveRange(0, length);
//            return result;
//        }
//        private static object GetDefault(Type type)
//        {
//            if (type.IsValueType)
//            {
//                return Activator.CreateInstance(type);
//            }
//            return null;
//        }
//    }
//}
