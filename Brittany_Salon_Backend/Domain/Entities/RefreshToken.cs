using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
 // permite almacenar y gestionar los refresh tokens en la base de datos, asociados a cada usuario o empleado, con su estado de validez y revocación. Es fundamental para implementar la funcionalidad de refresco de tokens JWT en el sistema de autenticación.
    [Table("RefreshToken")]
    public class RefreshToken
    {
        [Key]
        [Column("refreshTokenId")]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Column("userId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("userType")]
        public string UserType { get; set; } = string.Empty;

        [Required]
        [Column("expirationDate")]
        public DateTime ExpirationDate { get; set; }

        [Column("isRevoked")]
        public bool IsRevoked { get; set; } = false;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("revokedAt")]
        public DateTime? RevokedAt { get; set; }

        [MaxLength(255)]
        [Column("revocationReason")]
        public string? RevocationReason { get; set; }

        public RefreshToken() { }

        public RefreshToken(string token, int userId, string userType, DateTime expirationDate)
        {
            Token = token;
            UserId = userId;
            UserType = userType;
            ExpirationDate = expirationDate;
        }

        public bool IsValid => !IsRevoked && ExpirationDate > DateTime.UtcNow;

        public void Revoke(string reason = "")
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevocationReason = reason;
        }
    }
}
