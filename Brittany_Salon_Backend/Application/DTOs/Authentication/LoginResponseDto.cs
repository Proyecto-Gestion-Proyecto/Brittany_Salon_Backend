namespace Brittany_Salon_Backend.Application.DTOs.Authentication
{
    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;  // empleado o cliente
        
        // JWT Tokens
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpirationDate { get; set; }
        public DateTime RefreshTokenExpirationDate { get; set; }

        public string? Specialty { get; set; }

        public decimal? PendingBalance { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
