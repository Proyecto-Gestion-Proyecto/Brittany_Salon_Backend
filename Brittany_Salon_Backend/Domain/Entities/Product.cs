using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [Column("productId")]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("productName")]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("productDescription")]
        public string? ProductDescription { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [MaxLength(255)]
        [Column("imageUrl")]
        public string? ImageUrl { get; set; }

        [Column("expirationDate", TypeName = "date")]
        public DateTime? ExpirationDate { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("categoryId")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        public ICollection<AppointmentProduct> AppointmentProducts { get; set; } = new List<AppointmentProduct>();
    }
}
