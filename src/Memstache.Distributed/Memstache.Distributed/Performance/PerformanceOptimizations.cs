using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace MemStache.Distributed.Performance
{
    public class BatchOperationManager<TKey, TResult>
    {
        private readonly Serilog.ILogger _logger;
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TResult>>> _operations;

        public BatchOperationManager(Serilog.ILogger logger)
        {
            _logger = logger;
            _operations = new ConcurrentDictionary<TKey, Lazy<Task<TResult>>>();
        }

        public Task<TResult> GetOrAddAsync(TKey key, Func<TKey, Task<TResult>> operation)
        {
            _logger.Information("Attempting to get or add operation for key: {Key}", key);

            // Use Lazy<Task<TResult>> to ensure only one task is created for a given key
            var lazyTask = _operations.GetOrAdd(key, k =>
                new Lazy<Task<TResult>>(() => ExecuteOperationAsync(k, operation)));

            _logger.Information("Operation for key {Key} is in progress: {InProgress}", key, !lazyTask.IsValueCreated);

            return lazyTask.Value;
        }

        private async Task<TResult> ExecuteOperationAsync(TKey key, Func<TKey, Task<TResult>> operation)
        {
            try
            {
                _logger.Information("Starting execution of operation for key: {Key}", key);
                return await operation(key);
            }
            finally
            {
                _operations.TryRemove(key, out _); // Remove completed task from dictionary
                _logger.Information("Completed operation for key: {Key}", key);
            }
        }
    }

    public class MemoryEfficientByteArrayPool
    {
        private readonly ObjectPool<byte[]> _pool;
        private readonly ILogger _logger;

        public MemoryEfficientByteArrayPool(int arraySize, int maxArraysPerBucket, ILogger logger)
        {
            _pool = new DefaultObjectPool<byte[]>(new ByteArrayPooledObjectPolicy(arraySize), maxArraysPerBucket);
            _logger = logger;
        }

        public byte[] Rent()
        {
            var array = _pool.Get();
            _logger.Debug("Rented byte array from pool");
            return array;
        }

        public void Return(byte[] array)
        {
            _pool.Return(array);
            _logger.Debug("Returned byte array to pool");
        }

        private class ByteArrayPooledObjectPolicy : PooledObjectPolicy<byte[]>
        {
            private readonly int _arraySize;

            public ByteArrayPooledObjectPolicy(int arraySize)
            {
                _arraySize = arraySize;
            }

            public override byte[] Create() => new byte[_arraySize];

            public override bool Return(byte[] obj)
            {
                Array.Clear(obj, 0, obj.Length);
                return true;
            }
        }
    }
}
