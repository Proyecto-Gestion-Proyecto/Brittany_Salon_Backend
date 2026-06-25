using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brittany_Salon_Backend.Domain.Entities
{
    [Table("AppointmentService")]
    public class AppointmentService
    {
        [Key]
        [Column("appointmentServiceId")]
        public int AppointmentServiceId { get; set; }

        [Required]
        [Column("appointmentId")]
        public int AppointmentId { get; set; }

        [Required]
        [Column("serviceId")]
        public int ServiceId { get; set; }

        [Column("servicePrice", TypeName = "decimal(10,2)")]
        public decimal? ServicePrice { get; set; }

        public Appointment Appointment { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
