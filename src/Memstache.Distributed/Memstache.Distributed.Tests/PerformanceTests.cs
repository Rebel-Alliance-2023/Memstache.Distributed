using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using MemStache.Distributed.Performance;

namespace MemStache.Distributed.Tests.Unit
{
    public class PerformanceTests : IDisposable
    {
        private readonly ILogger<BatchOperationManager<string, int>> _logger;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
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
            _logger = loggerFactory.CreateLogger<BatchOperationManager<string, int>>();
        }

        [Fact]
        public async Task BatchOperationManager_ShouldPreventDuplicateOperations()
        {
            // Arrange
            var manager = new BatchOperationManager<string, int>(_serilogLogger);
            var operationCount = 0;

            Func<string, Task<int>> operation = async (key) =>
            {
                await Task.Delay(10); // Simulate some work
                return Interlocked.Increment(ref operationCount);
            };

            // Act
            var tasks = new List<Task<int>>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(manager.GetOrAddAsync("testKey", operation));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(1, operationCount);
            Assert.All(results, r => Assert.Equal(1, r));
        }

        [Fact]
        public void MemoryEfficientByteArrayPool_ShouldReuseArrays()
        {
            // Arrange
            var poolSize = 10;
            var arraySize = 1024;
            var pool = new MemoryEfficientByteArrayPool(arraySize, poolSize, _serilogLogger);

            // Act
            var arrays = new List<byte[]>();
            for (int i = 0; i < poolSize * 2; i++)
            {
                arrays.Add(pool.Rent());
            }

            foreach (var array in arrays)
            {
                pool.Return(array);
            }

            var reusedArrays = new List<byte[]>();
            for (int i = 0; i < poolSize; i++)
            {
                reusedArrays.Add(pool.Rent());
            }

            // Assert
            for (int i = 0; i < poolSize; i++)
            {
                Assert.Contains(reusedArrays[i], arrays);
            }
        }

        [Fact]
        public async Task ParallelOperations_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var manager = new BatchOperationManager<string, int>(_serilogLogger);
            var operationCount = 1000;
            var maxDurationMs = 2000; // 2 seconds

            Func<string, Task<int>> operation = async (key) =>
            {
                await Task.Delay(1); // Simulate minimal work
                return 1;
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<int>>();
            for (int i = 0; i < operationCount; i++)
            {
                tasks.Add(manager.GetOrAddAsync($"key{i % 10}", operation));
            }
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < maxDurationMs,
                $"Operation took {stopwatch.ElapsedMilliseconds}ms, which is more than the expected {maxDurationMs}ms");
        }

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
