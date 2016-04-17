using System;
using System.Text;
using Newtonsoft.Json;

namespace Rabbitual.Infrastructure
{
    public class JsonSerializer : ISerializer
    {
        public byte[] ToBytes<T>(T o)
        {
            var json = JsonConvert.SerializeObject(o);
            return Encoding.UTF8.GetBytes(json);
        }

        public T FromBytes<T>(byte[] body)
        {
            var message = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<T>(message);
        }
    }
}