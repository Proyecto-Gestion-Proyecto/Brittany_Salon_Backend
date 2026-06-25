namespace Brittany_Salon_Backend.Application.Tools.Models;

public enum RecoveryRequestStatus
{
    Accepted,
    RateLimited
}

public sealed class RecoveryRequestOutcome
{
    public required RecoveryRequestStatus Status { get; init; }
    public TimeSpan? RetryAfter { get; init; }
    public string Message { get; init; } =
        "Si el correo está registrado, recibirás un código de recuperación.";
}
