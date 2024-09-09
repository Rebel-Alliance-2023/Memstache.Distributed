using System;
using System.IO;
using System.IO.Compression;
using Serilog;

namespace MemStache.Distributed.Compression
{
    public class GzipCompressor : ICompressor
    {
        private readonly ILogger _logger;

        public GzipCompressor(ILogger logger)
        {
            _logger = logger;
        }

        public byte[] Compress(byte[] data)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error compressing data");
                throw;
            }
        }

        public byte[] Decompress(byte[] compressedData)
        {
            try
            {
                using var compressedStream = new MemoryStream(compressedData);
                using var decompressedStream = new MemoryStream();
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(decompressedStream);
                }
                return decompressedStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error decompressing data");
                throw;
            }
        }
    }
}
