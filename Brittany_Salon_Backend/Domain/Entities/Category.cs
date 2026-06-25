using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Category")]
    public class Category
    {
        [Key]
        [Column("categoryId")]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("categoryDescription")]
        public string? CategoryDescription { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
