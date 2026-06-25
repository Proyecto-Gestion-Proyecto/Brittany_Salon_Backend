namespace Brittany_Salon_Backend.Application.Tools.Models;

public enum RecoveryResetStatus
{
    Success,
    InvalidCode,
    ExpiredCode,
    NoActiveCode,
    AttemptLimitExceeded,
    UserNotFound,
    UserInactive
}

public sealed class RecoveryResetOutcome
{
    public required RecoveryResetStatus Status { get; init; }
    public string? Message { get; init; }
    public int RemainingAttempts { get; init; }
    public TimeSpan? RetryAfter { get; init; }
}
