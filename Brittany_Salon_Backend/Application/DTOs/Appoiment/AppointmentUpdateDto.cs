namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentUpdateDto
    {
        public DateTime StartTime { get; set; }

        public string? AppointmentStatus { get; set; }

        public int? HairLengthOption { get; set; } 

        public List<AppointmentServiceCreateDto> Services { get; set; } = new();
        public List<AppointmentProductCreateDto> Products { get; set; } = new();
    }
}
