namespace Brittany_Salon_Backend.Application.DTOs.Report
{
    public class TopClientDto
    {
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int CompletedAppointments { get; set; }
    } 
}
