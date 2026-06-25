using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        [Column("paymentId")]
        public int PaymentId { get; set; }

        [Required]
        [Column("appointmentId")]
        public int AppointmentId { get; set; }

        [Required]
        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [Column("paymentDate")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [MaxLength(50)]
        [Column("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty; // e.g., "Cash", "Card", "Transfer"

        [MaxLength(50)]
        [Column("paymentStatus")]
        public string? PaymentStatus { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        public Appointment? Appointment { get; set; }

        public Payment() { }
    }
}