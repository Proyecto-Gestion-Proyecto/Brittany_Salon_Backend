namespace Brittany_Salon_Backend.Application.Settings;

public sealed class RecoveryCodeSettings
{
    public const string SectionName = "RecoveryCode";

    public int CodeLength { get; set; } = 6;
    public int CodeLifetimeMinutes { get; set; } = 15;
    public int Pbkdf2Iterations { get; set; } = 200_000;
    public int SaltSizeBytes { get; set; } = 32;
    public int MaxValidationAttempts { get; set; } = 5;
    public int MaxRequestsPerEmailPerWindow { get; set; } = 3;
    public int MaxRequestsPerIpPerWindow { get; set; } = 10;
    public int RequestWindowMinutes { get; set; } = 15;
    public int CooldownSeconds { get; set; } = 60;
}
