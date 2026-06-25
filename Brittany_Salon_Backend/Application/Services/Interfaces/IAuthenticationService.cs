using Brittany_Salon_Backend.Application.DTOs.Authentication;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IAuthenticationService
    {

        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);

        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);

        Task RevokeRefreshTokenAsync(string refreshToken, string reason = "");

        Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, string? clientIp = null);

        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
    }
}
