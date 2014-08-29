namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.IO;
    using System.Text;
    using NEventStore.Serialization;
    using Newtonsoft.Json;

    public class GetEventStoreJsonSerializer : ISerialize
    {
        public delegate void Configure(JsonSerializerSettings settings);

        private static readonly byte[] BOM = Encoding.UTF8.GetPreamble();

        private readonly JsonSerializerSettings _serializerSettings;

        public GetEventStoreJsonSerializer(Configure configure = null)
        {
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            if (configure != null)
            {
                configure(_serializerSettings);
            }
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

            var raw = GetStringWithoutBOM<T>(buffer);

            return JsonConvert.DeserializeObject<T>(raw, _serializerSettings);
        }

        private static string GetStringWithoutBOM<T>(byte[] buffer)
        {
            var start = HasBOM(buffer) ? 3 : 0;

            return Encoding.UTF8.GetString(buffer, start, buffer.Length - start);
        }

        private static bool HasBOM(byte[] buffer)
        {
            return BOM[0] == buffer[0] && BOM[1] == buffer[1] && BOM[2] == buffer[2];
        }
    }
}