using System;
using System.Text.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using MemStache.Distributed.Serialization;

namespace MemStache.Distributed.Tests.Unit
{
    public class SerializationTests : IDisposable
    {
        private readonly SystemTextJsonSerializer _serializer;
        private readonly ITestOutputHelper _output;
        private readonly ILogger<SystemTextJsonSerializer> _logger;
        private readonly Serilog.Core.Logger _serilogLogger;

        public SerializationTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            // Use Serilog's LoggerFactory to create a Microsoft.Extensions.Logging.ILogger instance
            var loggerFactory = new SerilogLoggerFactory(_serilogLogger);
            _logger = loggerFactory.CreateLogger<SystemTextJsonSerializer>();

            // Initialize the SystemTextJsonSerializer with the Serilog logger
            _serializer = new SystemTextJsonSerializer(new JsonSerializerOptions(), _serilogLogger);
        }

        [Fact]
        public void Serialize_ShouldReturnValidBytes()
        {
            // Arrange
            var testObject = new TestObject { Name = "Test", Value = 42 };

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
            var testObject = new TestObject { Name = "Test", Value = 42 };
            var serialized = _serializer.Serialize(testObject);

            // Act
            var result = _serializer.Deserialize<TestObject>(serialized);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void SerializeAndDeserialize_ComplexObject_ShouldMaintainData()
        {
            // Arrange
            var complexObject = new ComplexObject
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Nested = new NestedObject { Name = "Nested Object", Value = 42 },
                List = new[] { "Item1", "Item2", "Item3" }
            };

            // Act
            byte[] serialized = _serializer.Serialize(complexObject);
            var deserialized = _serializer.Deserialize<ComplexObject>(serialized);

            // Assert
            Assert.Equal(complexObject.Id, deserialized.Id);
            Assert.Equal(complexObject.Date.ToString("O"), deserialized.Date.ToString("O"));
            Assert.Equal(complexObject.Nested.Name, deserialized.Nested.Name);
            Assert.Equal(complexObject.Nested.Value, deserialized.Nested.Value);
            Assert.Equal(complexObject.List.Length, deserialized.List.Length);
        }

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }

    public class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class ComplexObject
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public NestedObject Nested { get; set; } = new NestedObject();
        public string[] List { get; set; } = Array.Empty<string>();
    }

    public class NestedObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
