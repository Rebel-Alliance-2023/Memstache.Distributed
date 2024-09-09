using System.Text;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Compression;

namespace MemStache.Distributed.Tests.Unit
{
    public class CompressionTests
    {
        private readonly GzipCompressor _compressor;

        public CompressionTests()
        {
            var mockLogger = new Mock<ILogger<GzipCompressor>>();
            _compressor = new GzipCompressor((Serilog.ILogger)mockLogger.Object);
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
        public void Compress_EmptyData_ShouldReturnEmptyArray()
        {
            // Arrange
            var emptyData = new byte[0];

            // Act
            var compressed = _compressor.Compress(emptyData);

            // Assert
            Assert.Empty(compressed);
        }
    }
}
