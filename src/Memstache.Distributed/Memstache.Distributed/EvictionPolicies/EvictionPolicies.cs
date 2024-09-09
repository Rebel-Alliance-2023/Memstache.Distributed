using System;
using System.Collections.Concurrent;
using System.Linq;
using Serilog;

namespace MemStache.Distributed.EvictionPolicies
{
    public interface IEvictionPolicy
    {
        void RecordAccess(string key);
        string SelectVictim();
    }

    public class LruEvictionPolicy : IEvictionPolicy
    {
        private readonly ConcurrentDictionary<string, DateTime> _accessTimes = new();
        private readonly ILogger _logger;

        public LruEvictionPolicy(ILogger logger)
        {
            _logger = logger;
        }

        public void RecordAccess(string key)
        {
            _accessTimes[key] = DateTime.UtcNow;
        }

        public string SelectVictim()
        {
            try
            {
                var oldestKey = _accessTimes.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
                _logger.Information("LRU policy selected key {Key} for eviction", oldestKey);
                return oldestKey;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error selecting victim for LRU eviction");
                throw;
            }
        }
    }

    public class LfuEvictionPolicy : IEvictionPolicy
    {
        private readonly ConcurrentDictionary<string, int> _accessCounts = new();
        private readonly ILogger _logger;

        public LfuEvictionPolicy(ILogger logger)
        {
            _logger = logger;
        }

        public void RecordAccess(string key)
        {
            _accessCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
        }

        public string SelectVictim()
        {
            try
            {
                var leastFrequentKey = _accessCounts.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
                _logger.Information("LFU policy selected key {Key} for eviction", leastFrequentKey);
                return leastFrequentKey;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error selecting victim for LFU eviction");
                throw;
            }
        }
    }

    public class TimeBasedEvictionPolicy : IEvictionPolicy
    {
        private readonly ConcurrentDictionary<string, DateTime> _expirationTimes = new();
        private readonly ILogger _logger;

        public TimeBasedEvictionPolicy(ILogger logger)
        {
            _logger = logger;
        }

        public void RecordAccess(string key)
        {
            // This method is not used in time-based eviction
        }

        public void SetExpiration(string key, DateTime expirationTime)
        {
            _expirationTimes[key] = expirationTime;
        }

        public string SelectVictim()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKey = _expirationTimes.FirstOrDefault(kvp => kvp.Value <= now).Key;
                if (expiredKey != null)
                {
                    _logger.Information("Time-based policy selected key {Key} for eviction", expiredKey);
                }
                return expiredKey;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error selecting victim for time-based eviction");
                throw;
            }
        }
    }
}
