namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentAvailabilityRequestDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<int> ServiceIds { get; set; } = new();

        // Útil cuando se edita una cita: ignora conflictos con ella misma
        public int? ExcludeAppointmentId { get; set; }
    }
}
