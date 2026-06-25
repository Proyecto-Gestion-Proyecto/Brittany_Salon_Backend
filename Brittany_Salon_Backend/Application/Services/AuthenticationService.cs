using Brittany_Salon_Backend.Application.DTOs.Authentication;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Application.Tools.Models;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Persistence;
using Brittany_Salon_Backend.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BCrypt.Net;

namespace Brittany_Salon_Backend.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly IDevLogger _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IRecoveryFlow _recoveryFlow;

        public AuthenticationService(
            AppDbContext db,
            ITokenService tokenService,
            IDevLogger logger,
            IOptions<JwtSettings> jwtSettings,
            IRecoveryFlow recoveryFlow)
        {
            _db = db;
            _tokenService = tokenService;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _recoveryFlow = recoveryFlow;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                {
                    _logger?.LogWarning("Login: Email y contraseña son requeridos");
                    throw new ArgumentException("Email y contraseña son requeridos.");
                }

                var email = dto.Email.Trim().ToLower();

                var employee = await _db.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == email);

                if (employee != null)
                {
                    if (!employee.IsActive)
                    {
                        _logger?.LogWarning($"Login fallido: Empleado {email} inactivo");
                        throw new InvalidOperationException("Este empleado ha sido desactivado.");
                    }

                    if (!BCrypt.Net.BCrypt.Verify(dto.Password, employee.Password))
                    {
                        _logger?.LogWarning($"Login fallido: Contraseña incorrecta para empleado {email}");
                        throw new InvalidOperationException("Credenciales inválidas.");
                    }

                    return await CreateLoginResponse(employee.Id, employee.Email, "EMPLOYEE",
                        employee.Name, employee.Phone, employee.Specialty, employee.Image,
                        employee.IsActive, employee.DateCreated);
                }

                var client = await _db.Clients
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email);

                if (client != null)
                {
                    if (!client.IsActive)
                    {
                        _logger?.LogWarning($"Login fallido: Cliente {email} inactivo");
                        throw new InvalidOperationException("Esta cuenta ha sido desactivada.");
                    }

                    if (!BCrypt.Net.BCrypt.Verify(dto.Password, client.Password))
                    {
                        _logger?.LogWarning($"Login fallido: Contraseña incorrecta para cliente {email}");
                        throw new InvalidOperationException("Credenciales inválidas.");
                    }

                    return await CreateLoginResponse(client.ClientId, client.Email, "CLIENT",
                        client.Name, client.Phone, null, client.ImageUrl,
                        client.IsActive, client.CreatedAt, client.PendingBalance);
                }

                _logger?.LogWarning($"Login: Usuario no encontrado: {email}");
                throw new InvalidOperationException("Email no registrado.");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error en LoginAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                {
                    _logger?.LogWarning("RefreshToken: Token vacío");
                    throw new ArgumentException("El refresh token es requerido.");
                }
                var storedToken = await _db.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

                if (storedToken == null)
                {
                    _logger?.LogWarning($"RefreshToken: Token no encontrado en BD");
                    throw new InvalidOperationException("Refresh token inválido.");
                }
                if (storedToken.IsRevoked)
                {
                    _logger?.LogWarning($"RefreshToken: Token revocado para usuario {storedToken.UserId}");
                    throw new InvalidOperationException("Este refresh token ha sido revocado.");
                }
                if (storedToken.ExpirationDate < DateTime.UtcNow)
                {
                    _logger?.LogWarning($"RefreshToken: Token expirado para usuario {storedToken.UserId}");
                    throw new InvalidOperationException("Refresh token expirado.");
                }
                string email, role;
                if (storedToken.UserType == "EMPLOYEE")
                {
                    var employee = await _db.Employees.FindAsync(storedToken.UserId);
                    if (employee == null || !employee.IsActive)
                    {
                        _logger?.LogWarning($"RefreshToken: Empleado {storedToken.UserId} no encontrado o inactivo");
                        throw new InvalidOperationException("Usuario no encontrado o inactivo.");
                    }
                    email = employee.Email;
                    role = "EMPLOYEE";
                }
                else
                {
                    var client = await _db.Clients.FindAsync(storedToken.UserId);
                    if (client == null || !client.IsActive)
                    {
                        _logger?.LogWarning($"RefreshToken: Cliente {storedToken.UserId} no encontrado o inactivo");
                        throw new InvalidOperationException("Usuario no encontrado o inactivo.");
                    }
                    email = client.Email;
                    role = "CLIENT";
                }

                var newAccessToken = _tokenService.GenerateAccessToken(storedToken.UserId, email, role);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                var newAccessTokenExpirationDate = DateTime.UtcNow
                    .AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
                var newRefreshTokenExpirationDate = DateTime.UtcNow
                    .AddDays(_jwtSettings.RefreshTokenExpirationDays);

                storedToken.Revoke("Nuevo refresh token generado");

                var newRefreshTokenEntity = new Domain.Entities.RefreshToken(
                    newRefreshToken,
                    storedToken.UserId,
                    storedToken.UserType,
                    newRefreshTokenExpirationDate);

                _db.RefreshTokens.Add(newRefreshTokenEntity);
                await _db.SaveChangesAsync();

                _logger?.LogInfo($"Tokens refrescados exitosamente para usuario {storedToken.UserId}");

                return new RefreshTokenResponseDto
                {
                    UserId = storedToken.UserId,
                    Role = role,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpirationDate = newAccessTokenExpirationDate,
                    RefreshTokenExpirationDate = newRefreshTokenExpirationDate
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error en RefreshTokenAsync: {ex.Message}");
                throw;
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string reason = "")
        {
            try
            {
                var storedToken = await _db.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (storedToken != null)
                {
                    storedToken.Revoke(reason);
                    await _db.SaveChangesAsync();
                    _logger?.LogInfo($"Refresh token revocado: {reason}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al revocar refresh token: {ex.Message}");
            }
        }


        private async Task<LoginResponseDto> CreateLoginResponse(
            int userId, string email, string role, string name, string phone,
            string? specialty = null, string? imageUrl = null,
            bool isActive = true, DateTime? createdAt = null,
            decimal? pendingBalance = null)
        {
            var accessToken = _tokenService.GenerateAccessToken(userId, email, role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var accessTokenExpirationDate = DateTime.UtcNow
                .AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var refreshTokenExpirationDate = DateTime.UtcNow
                .AddDays(_jwtSettings.RefreshTokenExpirationDays);

            var refreshTokenEntity = new Domain.Entities.RefreshToken(
                refreshToken,
                userId,
                role,
                refreshTokenExpirationDate);

            _db.RefreshTokens.Add(refreshTokenEntity);
            await _db.SaveChangesAsync();

            _logger?.LogInfo($"Login exitoso para usuario {email} con rol {role}");

            return new LoginResponseDto
            {
                UserId = userId,
                Name = name,
                Email = email,
                Phone = phone,
                Role = role,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpirationDate = accessTokenExpirationDate,
                RefreshTokenExpirationDate = refreshTokenExpirationDate,
                Specialty = specialty,
                PendingBalance = pendingBalance,
                ImageUrl = imageUrl,
                IsActive = isActive,
                CreatedAt = createdAt
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, string? clientIp = null)
        {
            try
            {
                var outcome = await _recoveryFlow.RequestCodeAsync(dto, clientIp ?? string.Empty);

                if (outcome.Status == RecoveryRequestStatus.RateLimited)
                {
                    var retryAfter = outcome.RetryAfter ?? TimeSpan.FromSeconds(60);
                    throw new RateLimitExceededException(
                        $"Demasiadas solicitudes. Intenta en {(int)Math.Ceiling(retryAfter.TotalSeconds)} segundos.",
                        retryAfter);
                }
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error en ForgotPasswordAsync: {ex.Message}");
                throw;
            }
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            try
            {
                var outcome = await _recoveryFlow.ResetPasswordAsync(dto);

                switch (outcome.Status)
                {
                    case RecoveryResetStatus.Success:
                        return;

                    case RecoveryResetStatus.AttemptLimitExceeded:
                        throw new RateLimitExceededException(
                            outcome.Message ?? "Demasiados intentos. Intenta más tarde.",
                            outcome.RetryAfter);

                    case RecoveryResetStatus.ExpiredCode:
                        throw new InvalidOperationException(
                            outcome.Message ?? "El código ha expirado.")
                        {
                            Data = { ["HttpStatusCode"] = StatusCodes.Status410Gone }
                        };

                    case RecoveryResetStatus.InvalidCode:
                    case RecoveryResetStatus.NoActiveCode:
                    default:
                        throw new InvalidOperationException(
                            outcome.Message ?? "El código de verificación es inválido.");
                }
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error en ResetPasswordAsync: {ex.Message}");
                throw;
            }
        }
    }
}
