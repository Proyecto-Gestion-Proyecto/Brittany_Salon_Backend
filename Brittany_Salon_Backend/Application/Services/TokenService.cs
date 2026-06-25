using Brittany_Salon_Backend.Application.Services.Interfaces;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Brittany_Salon_Backend.Application.Services
{
    //Generar y validar tokens JWT para autenticación y autorización
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IDevLogger _logger;
        private readonly SymmetricSecurityKey _securityKey;
        private readonly SigningCredentials _signingCredentials;

        public TokenService(IOptions<JwtSettings> jwtSettings, IDevLogger logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;

            // Validar configuración
            _jwtSettings.Validate();

            // Crear clave simétrica
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        }

        //Generar un Access Token JWT con los datos del usuario   
        public string GenerateAccessToken(int userId, string email, string role)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    signingCredentials: _signingCredentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                var accessToken = tokenHandler.WriteToken(token);

                _logger?.LogInfo($"Access Token generado para usuario: {email}");

                return accessToken;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al generar Access Token: {ex.Message}");
                throw new InvalidOperationException("No se pudo generar el token de acceso", ex);
            }
        }

        //Generar un Refresh Token aleatorio y seguro
        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                }

                var refreshToken = Convert.ToBase64String(randomNumber);
                _logger?.LogInfo("Refresh Token generado exitosamente");
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al generar Refresh Token: {ex.Message}");
                throw new InvalidOperationException("No se pudo generar el token de refresco", ex);
            }
        }

        // Extraer los claims de un Access Token expirado para validar el Refresh Token
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Validar sin verificar expiración
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = false, // Importante: no validar expiración
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken? validatedToken);

                if (!(validatedToken is JwtSecurityToken jwtToken) ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger?.LogWarning("Token inválido: algoritmo incompatible");
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error al extraer claims del token: {ex.Message}");
                return null;
            }
        }

        //Valida que el token este activo
        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken? validatedToken);

                if (!(validatedToken is JwtSecurityToken))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Token inválido: {ex.Message}");
                return false;
            }
        }

        // Verifica si el token ha expirado
        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken == null)
                    return true;

                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }
}
