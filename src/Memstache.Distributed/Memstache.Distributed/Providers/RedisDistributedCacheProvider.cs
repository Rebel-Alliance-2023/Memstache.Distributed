using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Serilog;

namespace MemStache.Distributed.Providers
{
    public class RedisDistributedCacheProvider : IDistributedCacheProvider
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger _logger;
        private readonly RedisOptions _options;

        public RedisDistributedCacheProvider(IOptions<RedisOptions> options, ILogger logger)
        {
            _options = options.Value;
            _redis = ConnectionMultiplexer.Connect(_options.Configuration);
            _db = _redis.GetDatabase(_options.Database);
            _logger = logger;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? (byte[])value : null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving value for key {Key} from Redis", key);
                throw;
            }
        }

        public async Task SetAsync(string key, byte[] value, MemStacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                var expiry = GetExpiry(options);
                await _db.StringSetAsync(key, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting value for key {Key} in Redis", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error removing key {Key} from Redis", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking existence of key {Key} in Redis", key);
                throw;
            }
        }

        private TimeSpan? GetExpiry(MemStacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue)
            {
                return options.AbsoluteExpiration.Value;
            }
            else if (options.SlidingExpiration.HasValue)
            {
                return options.SlidingExpiration.Value;
            }
            return null;
        }
    }

    public class RedisOptions
    {
        public string Configuration { get; set; }
        public int Database { get; set; } = -1; // -1 is the default database in Redis
        public int ConnectTimeout { get; set; } = 5000; // 5 seconds
        public int SyncTimeout { get; set; } = 5000; // 5 seconds
        public bool AllowAdmin { get; set; } = false;
        public string Password { get; set; }
        public string[] EndPoints { get; set; }
        public bool Ssl { get; set; } = false;
        public string SslHost { get; set; }
    }
}
