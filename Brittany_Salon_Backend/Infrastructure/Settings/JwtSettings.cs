namespace Brittany_Salon_Backend.Infrastructure.Settings
{
  
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string SecretKey { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 15;

        public int RefreshTokenExpirationDays { get; set; } = 7;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SecretKey))
                throw new InvalidOperationException("JwtSettings.SecretKey no está configurado");

            if (SecretKey.Length < 32)
                throw new InvalidOperationException("JwtSettings.SecretKey debe tener al menos 32 caracteres");

            if (string.IsNullOrWhiteSpace(Issuer))
                throw new InvalidOperationException("JwtSettings.Issuer no está configurado");

            if (string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("JwtSettings.Audience no está configurado");

            if (AccessTokenExpirationMinutes <= 0)
                throw new InvalidOperationException("JwtSettings.AccessTokenExpirationMinutes debe ser mayor a 0");

            if (RefreshTokenExpirationDays <= 0)
                throw new InvalidOperationException("JwtSettings.RefreshTokenExpirationDays debe ser mayor a 0");
        }
    }
}
