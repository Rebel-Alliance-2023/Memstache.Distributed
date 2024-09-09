using System;
using System.Text.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Serialization;

namespace MemStache.Distributed.Tests.Unit
{
    public class SerializationTests
    {
        private readonly SystemTextJsonSerializer _serializer;

        public SerializationTests()
        {
            var mockLogger = new Mock<ILogger<SystemTextJsonSerializer>>();
            _serializer = new SystemTextJsonSerializer(new JsonSerializerOptions(), (Serilog.ILogger)mockLogger.Object);
        }

        [Fact]
        public void Serialize_ShouldReturnValidBytes()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 42 };

            // Act
            var result = _serializer.Serialize(testObject);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Deserialize_ShouldReturnValidObject()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 42 };
            var serialized = _serializer.Serialize(testObject);

            // Act
            var result = _serializer.Deserialize<dynamic>(serialized);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", (string)result.Name);
            Assert.Equal(42, (int)result.Value);
        }

        [Fact]
        public void SerializeAndDeserialize_ComplexObject_ShouldMaintainData()
        {
            // Arrange
            var complexObject = new
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Nested = new { Name = "Nested Object", Value = 42 },
                List = new[] { "Item1", "Item2", "Item3" }
            };

            // Act
            var serialized = _serializer.Serialize(complexObject);
            var deserialized = _serializer.Deserialize<dynamic>(serialized);

            // Assert
            Assert.Equal(complexObject.Id.ToString(), deserialized.Id.ToString());
            Assert.Equal(complexObject.Date.ToString("O"), DateTime.Parse(deserialized.Date.ToString()).ToString("O"));
            Assert.Equal(complexObject.Nested.Name, (string)deserialized.Nested.Name);
            Assert.Equal(complexObject.Nested.Value, (int)deserialized.Nested.Value);
            Assert.Equal(complexObject.List.Length, ((JsonElement)deserialized.List).GetArrayLength());
        }
    }
}
