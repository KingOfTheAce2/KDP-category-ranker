using KDP_CATEGORY_RANKERScraping.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KDP_CATEGORY_RANKERScraping.Services;

public class RateLimitService : IRateLimitService
{
    private readonly ILogger<RateLimitService> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<DateTime> _requestTimes;
    private readonly object _lock = new();
    
    private int _maxRequestsPerMinute;
    private int _delayBetweenRequestsMs;

    public RateLimitService(IConfiguration configuration, ILogger<RateLimitService> logger)
    {
        _logger = logger;
        _maxRequestsPerMinute = configuration.GetValue<int>("Scraping:MaxRequestsPerMinute", 10);
        _delayBetweenRequestsMs = configuration.GetValue<int>("Scraping:DelayBetweenRequestsMs", 2000);
        
        _semaphore = new SemaphoreSlim(configuration.GetValue<int>("Scraping:MaxConcurrency", 2));
        _requestTimes = new Queue<DateTime>();
        
        _logger.LogInformation("Rate limit service initialized: {MaxRequests}/min, {Delay}ms delay, {Concurrency} concurrent",
            _maxRequestsPerMinute, _delayBetweenRequestsMs, configuration.GetValue<int>("Scraping:MaxConcurrency", 2));
    }

    public async Task WaitIfNeededAsync()
    {
        await _semaphore.WaitAsync();
        
        try
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddMinutes(-1);
                
                // Remove old requests
                while (_requestTimes.Count > 0 && _requestTimes.Peek() < cutoff)
                {
                    _requestTimes.Dequeue();
                }
                
                // Check if we need to wait
                if (_requestTimes.Count >= _maxRequestsPerMinute)
                {
                    var oldestRequest = _requestTimes.Peek();
                    var waitTime = oldestRequest.AddMinutes(1) - now;
                    
                    if (waitTime > TimeSpan.Zero)
                    {
                        _logger.LogDebug("Rate limit reached, waiting {WaitTime}ms", waitTime.TotalMilliseconds);
                        Task.Delay(waitTime).Wait();
                    }
                }
                
                _requestTimes.Enqueue(now);
            }
            
            // Add base delay between requests
            if (_delayBetweenRequestsMs > 0)
            {
                var jitter = Random.Shared.Next(0, _delayBetweenRequestsMs / 2);
                await Task.Delay(_delayBetweenRequestsMs + jitter);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> ExecuteWithRateLimitAsync<T>(Func<Task<T>> operation)
    {
        await WaitIfNeededAsync();
        
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rate-limited operation");
            throw;
        }
    }

    public void UpdateSettings(int maxRequestsPerMinute, int delayBetweenRequestsMs)
    {
        lock (_lock)
        {
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _delayBetweenRequestsMs = delayBetweenRequestsMs;
            
            _logger.LogInformation("Rate limit settings updated: {MaxRequests}/min, {Delay}ms delay",
                maxRequestsPerMinute, delayBetweenRequestsMs);
        }
    }
}