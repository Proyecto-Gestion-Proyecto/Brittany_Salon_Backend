using System.Security.Cryptography;
using System.Text;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Application.Tools.Models;
using Microsoft.Extensions.Options;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class RecoveryCodeValidator : IRecoveryCodeValidator
{
    private readonly IRecoveryCodeStore _store;
    private readonly IRecoveryCodeRateLimiter _rateLimiter;
    private readonly RecoveryCodeSettings _settings;

    public RecoveryCodeValidator(
        IRecoveryCodeStore store,
        IRecoveryCodeRateLimiter rateLimiter,
        IOptions<RecoveryCodeSettings> settings)
    {
        _store = store;
        _rateLimiter = rateLimiter;
        _settings = settings.Value;
    }

    public RecoveryValidationResult Validate(string email, string providedCode)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.NoActiveCode,
                RemainingAttempts = 0
            };
        }

        if (string.IsNullOrWhiteSpace(providedCode))
        {
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.InvalidCode,
                RemainingAttempts = 0
            };
        }

        if (!_rateLimiter.TryAcquireAttemptSlot(email, out var remaining))
        {
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.AttemptLimitExceeded,
                RemainingAttempts = 0
            };
        }

        var record = _store.Get(email);
        if (record is null)
        {
            _rateLimiter.RecordFailedAttempt(email);
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.NoActiveCode,
                RemainingAttempts = remaining - 1
            };
        }

        var now = DateTime.UtcNow;
        if (now > record.ExpiresAtUtc)
        {
            _store.Remove(email);
            _rateLimiter.RecordFailedAttempt(email);
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.Expired,
                RemainingAttempts = remaining - 1
            };
        }

        var computed = RecoveryCodeGenerator.ComputePbkdf2(
            providedCode,
            record.Salt,
            record.Pbkdf2Iterations);

        var storedBytes = Encoding.ASCII.GetBytes(record.CodeHash);
        var computedBytes = Encoding.ASCII.GetBytes(computed);

        var matches = storedBytes.Length == computedBytes.Length
                      && CryptographicOperations.FixedTimeEquals(storedBytes, computedBytes);

        if (!matches)
        {
            _rateLimiter.RecordFailedAttempt(email);
            return new RecoveryValidationResult
            {
                Status = RecoveryValidationStatus.InvalidCode,
                RemainingAttempts = remaining - 1
            };
        }

        return new RecoveryValidationResult
        {
            Status = RecoveryValidationStatus.Valid,
            RemainingAttempts = _settings.MaxValidationAttempts
        };
    }

    public void RegisterAttempt(string email, bool success)
    {
        if (success)
        {
            _rateLimiter.ResetAttempts(email);
        }
        else
        {
            _rateLimiter.RecordFailedAttempt(email);
        }
    }

    public void Invalidate(string email)
    {
        _store.Remove(email);
        _rateLimiter.ResetAttempts(email);
    }
}
