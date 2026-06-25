using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Category
{
    public class CategoryCreateDto
    {
        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CategoryDescription { get; set; }
    }
}
