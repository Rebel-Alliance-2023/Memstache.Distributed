using System;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Serilog;

namespace MemStache.Distributed.Resilience
{
    public class ResiliencePolicies
    {
        private readonly ILogger _logger;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ResiliencePolicies(ILogger logger)
        {
            _logger = logger;

            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, breakDuration) =>
                    {
                        _logger.Warning(ex, "Circuit breaker opened for {BreakDuration}", breakDuration);
                    },
                    onReset: () =>
                    {
                        _logger.Information("Circuit breaker reset");
                    });

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, timeSpan, retryCount, context) =>
                    {
                        _logger.Warning(ex, "Retry attempt {RetryCount} after {RetryInterval}s delay", retryCount, timeSpan.TotalSeconds);
                    });
        }

        public Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> action, string operationName)
        {
            return Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy)
                .ExecuteAsync(async () =>
                {
                    try
                    {
                        _logger.Information("Executing {OperationName}", operationName);
                        return await action();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error executing {OperationName}", operationName);
                        throw;
                    }
                });
        }
    }
}
