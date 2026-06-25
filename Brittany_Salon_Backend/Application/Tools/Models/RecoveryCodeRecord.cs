namespace Brittany_Salon_Backend.Application.Tools.Models;

public sealed class RecoveryCodeRecord
{
    public required string CodeHash { get; init; }
    public required byte[] Salt { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required int Pbkdf2Iterations { get; init; }
}
