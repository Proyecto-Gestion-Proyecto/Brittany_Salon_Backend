using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Client")]
    public class Clients
    {
        [Key]
        [Column("clientId")]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("passwordHash")]
        public string Password { get; set; } = string.Empty;

        [Column("pendingBalance")]
        public decimal PendingBalance { get; set; } = 0;

        [MaxLength(255)]
        [Column("imageUrl")]
        public string? ImageUrl { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        [Column("createdAt")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public Clients() { }
    }
}
