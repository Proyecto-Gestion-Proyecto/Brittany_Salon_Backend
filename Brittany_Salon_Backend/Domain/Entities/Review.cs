using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Review")]
    public class Review
    {
        [Key]
        [Column("reviewId")]
        public int ReviewId { get; set; }

        [MaxLength(255)]
        [Column("comment")]
        public string? Comment { get; set; }

        [Required]
        [Column("rating")]
        public int Rating { get; set; }

        [MaxLength(255)]
        [Column("imageUrl")]
        public string? ImageUrl { get; set; }

        [MaxLength(255)]
        [Column("response")]
        public string? Response { get; set; }

        [Column("reviewDate", TypeName = "date")]
        public DateTime ReviewDate { get; set; } = DateTime.Now;

        [Required]
        [Column("clientId")]
        public int ClientId { get; set; }

        [Column("employeeId")]
        public int? EmployeeId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Clients? Client { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }
    }
}
