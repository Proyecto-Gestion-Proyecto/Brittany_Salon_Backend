using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("Appointment")]
    public class Appointment
    {
        [Key]
        [Column("appointmentId")]
        public int AppointmentId { get; set; }

        [Required]
        [Column("appointmentDate", TypeName = "date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Column("startTime")]
        public DateTime StartTime { get; set; }

        [Required]
        [Column("endTime")]
        public DateTime EndTime { get; set; }

        [MaxLength(50)]
        [Column("appointmentStatus")]
        public string? AppointmentStatus { get; set; }

        [Column("totalCost", TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; }

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        [Column("hairLengthOption")]
        public int? HairLengthOption { get; set; }

        [Required]
        [Column("clientId")]
        public int ClientId { get; set; }

        // Navegación
        [ForeignKey(nameof(ClientId))]
        public Clients Client { get; set; } = null!;

        public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
        public ICollection<AppointmentProduct> AppointmentProducts { get; set; } = new List<AppointmentProduct>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
