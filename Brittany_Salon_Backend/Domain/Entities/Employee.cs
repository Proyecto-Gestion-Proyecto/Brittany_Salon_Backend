using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [Column("employeeId")]
        public int Id { get; set; }

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

        [MaxLength(255)]
        [Column("imageUrl")]
        public string? Image { get; set; }

        [MaxLength(100)]
        [Column("specialty")]
        public string? Specialty { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        [Column("createdAt")]
        public DateTime? DateCreated { get; set; } = DateTime.Now;

        public Employee() { }
    }
}
