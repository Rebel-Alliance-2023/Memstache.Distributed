using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace MemStache.Distributed.Performance
{
    public class BatchOperationManager<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<TValue>> _pendingOperations = new();
        private readonly ILogger _logger;

        public BatchOperationManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
        {
            TaskCompletionSource<TValue> tcs = _pendingOperations.GetOrAdd(key, _ => new TaskCompletionSource<TValue>());

            if (tcs.Task.IsCompleted)
            {
                return await tcs.Task;
            }

            try
            {
                if (_pendingOperations.TryGetValue(key, out var existingTcs) && existingTcs == tcs)
                {
                    TValue value = await valueFactory(key);
                    tcs.TrySetResult(value);
                }

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in batch operation for key {Key}", key);
                tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _pendingOperations.TryRemove(key, out _);
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
