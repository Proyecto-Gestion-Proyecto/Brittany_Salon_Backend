namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryCodeRateLimiter
{
    bool TryAcquireRequestSlot(string email, string ip, out TimeSpan? retryAfter);
    bool TryAcquireAttemptSlot(string email, out int remainingAttempts);
    void RecordFailedAttempt(string email);
    void ResetAttempts(string email);
}
