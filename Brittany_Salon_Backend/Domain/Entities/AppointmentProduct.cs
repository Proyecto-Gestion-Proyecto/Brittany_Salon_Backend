using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("AppointmentProduct")]
    public class AppointmentProduct
    {
        [Key]
        [Column("appointmentProductId")]
        public int AppointmentProductId { get; set; }

        [Required]
        [Column("appointmentId")]
        public int AppointmentId { get; set; }

        [Required]
        [Column("productId")]
        public int ProductId { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [ForeignKey(nameof(AppointmentId))]
        public Appointment Appointment { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
