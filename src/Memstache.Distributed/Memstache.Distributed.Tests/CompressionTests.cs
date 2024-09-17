using System.Text;
using Xunit;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Compression;
using System.IO.Compression;

namespace MemStache.Distributed.Tests.Unit
{
    public class CompressionTests : IDisposable
    {
        private readonly GzipCompressor _compressor;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ITestOutputHelper _output;

        public CompressionTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            // Use Serilog logger directly for GzipCompressor
            _compressor = new GzipCompressor(_serilogLogger);
        }

        [Fact]
        public void Compress_ShouldReduceDataSize()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes(new string('a', 1000));

            // Act
            var compressed = _compressor.Compress(data);

            // Assert
            Assert.True(compressed.Length < data.Length);
        }

        [Fact]
        public void Decompress_ShouldRestoreOriginalData()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Test data for compression");
            var compressed = _compressor.Compress(originalData);

            // Act
            var decompressed = _compressor.Decompress(compressed);

            // Assert
            Assert.Equal(originalData, decompressed);
        }

        [Fact]
        public void CompressAndDecompress_LargeData_ShouldMaintainIntegrity()
        {
            // Arrange
            var largeData = Encoding.UTF8.GetBytes(new string('a', 1000000)); // 1MB of 'a'

            // Act
            var compressed = _compressor.Compress(largeData);
            var decompressed = _compressor.Decompress(compressed);

            // Assert
            Assert.Equal(largeData, decompressed);
        }

        [Fact]
        public void Compress_EmptyData_ShouldReturnValidGzip()
        {
            // Arrange
            var emptyData = new byte[0];

            // Act
            var compressed = _compressor.Compress(emptyData);

            // Assert
            Assert.NotNull(compressed);
            Assert.NotEmpty(compressed);

            // Decompress to verify
            using (var compressedStream = new MemoryStream(compressed))
            using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                decompressionStream.CopyTo(resultStream);
                var decompressedData = resultStream.ToArray();
                Assert.Empty(decompressedData);
            }
        }




        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
