using Brittany_Salon_Backend.Application.DTOs.Authentication;
using Brittany_Salon_Backend.Application.Exceptions;
using Brittany_Salon_Backend.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login de usuario (Employee o Client)
        /// </summary>
        /// <param name="dto">Email y contraseña del usuario</param>
        /// <returns>Tokens JWT y datos del usuario</returns>
        /// <response code="200">Login exitoso, retorna AccessToken y RefreshToken</response>
        /// <response code="401">Credenciales inválidas</response>
        /// <response code="400">Datos inválidos</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse 
                    { 
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(new ErrorResponse { Message = "Error interno en el servidor" });
            }
            catch (ArgumentException)
            {
                return BadRequest(new ErrorResponse { Message = "Error interno en el servidor" });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Error interno en el servidor" });
            }
        }

        /// <summary>
        /// Refresca el Access Token usando un Refresh Token válido
        /// </summary>
        /// <param name="dto">Contiene el Refresh Token</param>
        /// <returns>Nuevos tokens JWT</returns>
        /// <response code="200">Refresh exitoso, retorna nuevos tokens</response>
        /// <response code="401">Refresh token inválido o expirado</response>
        /// <response code="400">Datos inválidos</response>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RefreshTokenResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse 
                    { 
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _authService.RefreshTokenAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Unauthorized(new ErrorResponse { Message = "Error interno en el servidor" });
            }
            catch (ArgumentException)
            {
                return BadRequest(new ErrorResponse { Message = "Error interno en el servidor" });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Error interno en el servidor" });
            }
        }

        /// <summary>
        /// Revoca la sesión actual (logout)
        /// </summary>
        /// <param name="dto">Contiene el Refresh Token a revocar</param>
        /// <returns>Confirmación de logout</returns>
        /// <response code="200">Logout exitoso</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                await _authService.RevokeRefreshTokenAsync(dto.RefreshToken, "Logout del usuario");
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Error al cerrar sesión" });
            }
        }

        /// <summary>
        /// Envía un código de recuperación de contraseña al correo del usuario.
        /// La respuesta es siempre la misma para evitar enumeración de cuentas.
        /// </summary>
        /// <param name="dto">Correo electrónico del usuario</param>
        /// <returns>Confirmación genérica de solicitud</returns>
        /// <response code="200">Solicitud procesada (con o sin envío real)</response>
        /// <response code="400">Datos de entrada inválidos</response>
        /// <response code="429">Demasiadas solicitudes</response>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                await _authService.ForgotPasswordAsync(dto, clientIp);

                return Ok(new { message = "Si el correo está registrado, recibirás un código de recuperación." });
            }
            catch (RateLimitExceededException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Error interno en el servidor" });
            }
        }

        /// <summary>
        /// Restablece la contraseña usando el código de verificación recibido por correo
        /// </summary>
        /// <param name="dto">Email, código de verificación y nueva contraseña</param>
        /// <returns>Confirmación de restablecimiento</returns>
        /// <response code="200">Contraseña restablecida exitosamente</response>
        /// <response code="400">Código inválido</response>
        /// <response code="410">Código expirado</response>
        /// <response code="429">Demasiados intentos</response>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                await _authService.ResetPasswordAsync(dto);
                return Ok(new { message = "Contraseña restablecida exitosamente." });
            }
            catch (RateLimitExceededException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new ErrorResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                var status = ex.Data.Contains("HttpStatusCode")
                    ? (int)ex.Data["HttpStatusCode"]!
                    : StatusCodes.Status400BadRequest;
                return StatusCode(status, new ErrorResponse { Message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { Message = "Error interno en el servidor" });
            }
        }
    }

    /// <summary>
    /// Respuesta de error estándar
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public List<string>? Errors { get; set; }
    }
}
