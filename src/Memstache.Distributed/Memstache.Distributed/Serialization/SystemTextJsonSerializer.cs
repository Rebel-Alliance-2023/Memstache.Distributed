using System;
using System.Text.Json;
using Serilog;

namespace MemStache.Distributed.Serialization
{
    public class SystemTextJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions _options;
        private readonly ILogger _logger;

        public SystemTextJsonSerializer(JsonSerializerOptions options, ILogger logger)
        {
            _options = options ?? new JsonSerializerOptions();
            _logger = logger;
        }

        public byte[] Serialize<T>(T value)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(value, _options);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error serializing object of type {Type}", typeof(T).Name);
                throw;
            }
        }

        public T Deserialize<T>(byte[] data)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(data, _options);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deserializing object to type {Type}", typeof(T).Name);
                throw;
            }
        }
    }
}
