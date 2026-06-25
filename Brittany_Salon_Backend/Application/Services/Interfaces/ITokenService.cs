using System.Security.Claims;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
 
    public interface ITokenService
    {
  
        string GenerateAccessToken(int userId, string email, string role);

        string GenerateRefreshToken();

        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        bool ValidateToken(string token);
    }
}
