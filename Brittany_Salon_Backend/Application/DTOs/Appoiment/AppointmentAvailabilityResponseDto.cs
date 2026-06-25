namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentAvailabilityResponseDto
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
