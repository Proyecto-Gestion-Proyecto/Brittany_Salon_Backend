namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentDetailDto
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? AppointmentStatus { get; set; }
        public decimal? TotalCost { get; set; }
        public bool IsActive { get; set; }

        public int ClientId { get; set; }

        public int? HairLengthOption { get; set; }
        public ClientMiniDto Client { get; set; } = new();

        public List<AppointmentServiceDetailDto> Services { get; set; } = new();
        public List<AppointmentProductDetailDto> Products { get; set; } = new();
    }

    public class ClientMiniDto
    {
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class AppointmentServiceDetailDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
    }

    public class AppointmentProductDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

}
