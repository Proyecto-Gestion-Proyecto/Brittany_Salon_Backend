namespace Brittany_Salon_Backend.Application.Exceptions;

public class RateLimitExceededException : Exception
{
    public TimeSpan? RetryAfter { get; }

    public RateLimitExceededException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }
}
