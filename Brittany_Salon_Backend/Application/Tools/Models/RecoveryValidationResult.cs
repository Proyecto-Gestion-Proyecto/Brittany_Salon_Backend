namespace Brittany_Salon_Backend.Application.Tools.Models;

public enum RecoveryValidationStatus
{
    Valid,
    InvalidCode,
    Expired,
    AttemptLimitExceeded,
    NoActiveCode
}

public sealed class RecoveryValidationResult
{
    public required RecoveryValidationStatus Status { get; init; }
    public required int RemainingAttempts { get; init; }
    public TimeSpan? RetryAfter { get; init; }
}
