namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentReadDto
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? AppointmentStatus { get; set; }
        public decimal? TotalCost { get; set; }
        public bool IsActive { get; set; }

        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public int? HairLengthOption { get; set; }
    }
}
