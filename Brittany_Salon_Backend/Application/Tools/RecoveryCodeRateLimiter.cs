using System.Collections.Concurrent;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Microsoft.Extensions.Options;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class RecoveryCodeRateLimiter : IRecoveryCodeRateLimiter
{
    private readonly RecoveryCodeSettings _settings;
    private readonly ConcurrentDictionary<string, RequestBucket> _emailBuckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RequestBucket> _ipBuckets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AttemptBucket> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public RecoveryCodeRateLimiter(IOptions<RecoveryCodeSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool TryAcquireRequestSlot(string email, string ip, out TimeSpan? retryAfter)
    {
        retryAfter = null;

        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailBucket = _emailBuckets.AddOrUpdate(
                email,
                _ => CreateRequestBucket(now),
                (_, current) => AdvanceRequestBucket(current, now));

            if (emailBucket.Count >= _settings.MaxRequestsPerEmailPerWindow)
            {
                retryAfter = emailBucket.WindowEnd - now;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.Zero;
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var ipBucket = _ipBuckets.AddOrUpdate(
                ip,
                _ => CreateRequestBucket(now),
                (_, current) => AdvanceRequestBucket(current, now));

            if (ipBucket.Count >= _settings.MaxRequestsPerIpPerWindow)
            {
                retryAfter = ipBucket.WindowEnd - now;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.Zero;
                return false;
            }
        }

        return true;
    }

    public bool TryAcquireAttemptSlot(string email, out int remainingAttempts)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            remainingAttempts = 0;
            return false;
        }

        var now = DateTime.UtcNow;
        var bucket = _attempts.AddOrUpdate(
            email,
            _ => new AttemptBucket { Count = 0, WindowStart = now },
            (_, current) =>
            {
                if (now - current.WindowStart > TimeSpan.FromMinutes(_settings.RequestWindowMinutes))
                {
                    return new AttemptBucket { Count = 0, WindowStart = now };
                }
                return current;
            });

        if (bucket.Count >= _settings.MaxValidationAttempts)
        {
            remainingAttempts = 0;
            return false;
        }

        remainingAttempts = _settings.MaxValidationAttempts - bucket.Count;
        return true;
    }

    public void RecordFailedAttempt(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        _attempts.AddOrUpdate(
            email,
            _ => new AttemptBucket { Count = 1, WindowStart = DateTime.UtcNow },
            (_, current) =>
            {
                if (DateTime.UtcNow - current.WindowStart > TimeSpan.FromMinutes(_settings.RequestWindowMinutes))
                {
                    return new AttemptBucket { Count = 1, WindowStart = DateTime.UtcNow };
                }
                current.Count += 1;
                return current;
            });
    }

    public void ResetAttempts(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        _attempts.TryRemove(email, out _);
    }

    private RequestBucket CreateRequestBucket(DateTime now)
    {
        var windowStart = now;
        var windowEnd = now.AddMinutes(_settings.RequestWindowMinutes);
        return new RequestBucket { Count = 1, WindowStart = windowStart, WindowEnd = windowEnd };
    }

    private RequestBucket AdvanceRequestBucket(RequestBucket current, DateTime now)
    {
        if (now >= current.WindowEnd)
        {
            return new RequestBucket
            {
                Count = 1,
                WindowStart = now,
                WindowEnd = now.AddMinutes(_settings.RequestWindowMinutes)
            };
        }

        current.Count += 1;
        return current;
    }

    private sealed class RequestBucket
    {
        public int Count;
        public DateTime WindowStart;
        public DateTime WindowEnd;
    }

    private sealed class AttemptBucket
    {
        public int Count;
        public DateTime WindowStart;
    }
}
