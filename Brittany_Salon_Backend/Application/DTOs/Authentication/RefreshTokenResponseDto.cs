namespace Brittany_Salon_Backend.Application.DTOs.Authentication
{
    // DTO para la respuesta al refrescar el token
    // Acá estan los nuevos tokens generados
    public class RefreshTokenResponseDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpirationDate { get; set; }
        public DateTime RefreshTokenExpirationDate { get; set; }
    }
}
