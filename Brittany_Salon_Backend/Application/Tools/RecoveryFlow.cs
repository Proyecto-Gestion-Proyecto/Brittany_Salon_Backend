using BCrypt.Net;
using Brittany_Salon_Backend.Application.DTOs.Authentication;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Application.Tools.Models;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class RecoveryFlow : IRecoveryFlow
{
    private readonly AppDbContext _db;
    private readonly IRecoveryCodeGenerator _generator;
    private readonly IRecoveryCodeStore _store;
    private readonly IRecoveryCodeValidator _validator;
    private readonly IRecoveryCodeSender _sender;
    private readonly IRecoveryCodeRateLimiter _rateLimiter;
    private readonly RecoveryCodeSettings _settings;
    private readonly IDevLogger _logger;

    public RecoveryFlow(
        AppDbContext db,
        IRecoveryCodeGenerator generator,
        IRecoveryCodeStore store,
        IRecoveryCodeValidator validator,
        IRecoveryCodeSender sender,
        IRecoveryCodeRateLimiter rateLimiter,
        IOptions<RecoveryCodeSettings> settings,
        IDevLogger logger)
    {
        _db = db;
        _generator = generator;
        _store = store;
        _validator = validator;
        _sender = sender;
        _rateLimiter = rateLimiter;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RecoveryRequestOutcome> RequestCodeAsync(
        ForgotPasswordRequestDto dto,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return new RecoveryRequestOutcome
            {
                Status = RecoveryRequestStatus.RateLimited
            };
        }

        if (!_rateLimiter.TryAcquireRequestSlot(email, clientIp, out var retryAfter))
        {
            _logger.LogWarning(
                $"ForgotPassword rate-limited para email/ip. RetryAfter={retryAfter}");

            return new RecoveryRequestOutcome
            {
                Status = RecoveryRequestStatus.RateLimited,
                RetryAfter = retryAfter
            };
        }

        var userInfo = await ResolveUserAsync(email, cancellationToken);
        if (userInfo is null)
        {
            _logger.LogInfo($"ForgotPassword request para email no registrado: {email}");
            return new RecoveryRequestOutcome
            {
                Status = RecoveryRequestStatus.Accepted
            };
        }

        var (userName, isActive) = userInfo.Value;
        if (!isActive)
        {
            _logger.LogInfo($"ForgotPassword request para cuenta inactiva: {email}");
            return new RecoveryRequestOutcome
            {
                Status = RecoveryRequestStatus.Accepted
            };
        }

        await IssueNewCodeAsync(email, userName, cancellationToken);

        return new RecoveryRequestOutcome
        {
            Status = RecoveryRequestStatus.Accepted
        };
    }

    public async Task<RecoveryResetOutcome> ResetPasswordAsync(
        ResetPasswordRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(dto.Code) ||
            string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return new RecoveryResetOutcome
            {
                Status = RecoveryResetStatus.InvalidCode,
                Message = "Código de verificación inválido."
            };
        }

        var validation = _validator.Validate(email, dto.Code);

        switch (validation.Status)
        {
            case RecoveryValidationStatus.AttemptLimitExceeded:
                return new RecoveryResetOutcome
                {
                    Status = RecoveryResetStatus.AttemptLimitExceeded,
                    Message = "Demasiados intentos. Intenta más tarde.",
                    RetryAfter = validation.RetryAfter
                };

            case RecoveryValidationStatus.Expired:
                return new RecoveryResetOutcome
                {
                    Status = RecoveryResetStatus.ExpiredCode,
                    Message = "El código ha expirado. Solicita uno nuevo.",
                    RemainingAttempts = validation.RemainingAttempts
                };

            case RecoveryValidationStatus.NoActiveCode:
                return new RecoveryResetOutcome
                {
                    Status = RecoveryResetStatus.NoActiveCode,
                    Message = "No hay un código activo. Solicita uno nuevo.",
                    RemainingAttempts = validation.RemainingAttempts
                };

            case RecoveryValidationStatus.InvalidCode:
                return new RecoveryResetOutcome
                {
                    Status = RecoveryResetStatus.InvalidCode,
                    Message = "El código ingresado es incorrecto.",
                    RemainingAttempts = validation.RemainingAttempts
                };
        }

        var userInfo = await ResolveUserAsync(email, cancellationToken);
        if (userInfo is null)
        {
            _validator.Invalidate(email);
            return new RecoveryResetOutcome
            {
                Status = RecoveryResetStatus.UserNotFound,
                Message = "No se pudo completar la operación."
            };
        }

        var (userName, isActive) = userInfo.Value;
        if (!isActive)
        {
            _validator.Invalidate(email);
            return new RecoveryResetOutcome
            {
                Status = RecoveryResetStatus.UserInactive,
                Message = "No se pudo completar la operación."
            };
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        var updated = await UpdatePasswordAsync(email, hashedPassword, cancellationToken);

        if (!updated)
        {
            _validator.Invalidate(email);
            return new RecoveryResetOutcome
            {
                Status = RecoveryResetStatus.UserNotFound,
                Message = "No se pudo completar la operación."
            };
        }

        _validator.Invalidate(email);

        _logger.LogInfo($"Contraseña restablecida exitosamente para: {email}");

        return new RecoveryResetOutcome
        {
            Status = RecoveryResetStatus.Success,
            Message = "Contraseña restablecida exitosamente."
        };
    }

    private async Task IssueNewCodeAsync(string email, string userName, CancellationToken cancellationToken)
    {
        var code = _generator.Generate(_settings.CodeLength);
        var secret = _generator.CreateSecret(code);

        var record = new RecoveryCodeRecord
        {
            CodeHash = secret.Hash,
            Salt = secret.Salt,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.CodeLifetimeMinutes),
            CreatedAtUtc = DateTime.UtcNow,
            Pbkdf2Iterations = _settings.Pbkdf2Iterations
        };

        _store.Save(email, record);
        _validator.Invalidate(email);

        try
        {
            await _sender.SendAsync(email, userName, code, cancellationToken);
        }
        catch (Exception ex)
        {
            _store.Remove(email);
            _logger.LogError($"Error enviando código de recuperación: {ex.Message}", ex);
            throw;
        }
    }

    private async Task<(string UserName, bool IsActive)?> ResolveUserAsync(string email, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email, cancellationToken);

        if (employee is not null)
        {
            return (employee.Name, employee.IsActive);
        }

        var client = await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email, cancellationToken);

        if (client is not null)
        {
            return (client.Name, client.IsActive);
        }

        return null;
    }

    private async Task<bool> UpdatePasswordAsync(string email, string hashedPassword, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email, cancellationToken);

        if (employee is not null)
        {
            employee.Password = hashedPassword;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email, cancellationToken);

        if (client is not null)
        {
            client.Password = hashedPassword;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }
}
