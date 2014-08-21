namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.IO;
    using System.Text;
    using NEventStore.Serialization;
    using Newtonsoft.Json;

    public class GetEventStoreJsonSerializer : ISerialize
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public GetEventStoreJsonSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            };
        }
        public void Serialize<T>(Stream output, T graph)
        {
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(graph, _serializerSettings));

            output.Write(buffer, 0, buffer.Length);
        }

        public T Deserialize<T>(Stream input)
        {
            var buffer = new Byte[input.Length - input.Position];

            input.Read(buffer, 0, buffer.Length);

            var raw = Encoding.UTF8.GetString(buffer);

            return JsonConvert.DeserializeObject<T>(raw, _serializerSettings);
        }
    }
}