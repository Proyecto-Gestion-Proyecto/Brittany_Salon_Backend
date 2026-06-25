namespace Brittany_Salon_Backend.Application.DTOs.Service
{
    public class FeaturedServiceReadDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public string? ImageUrl { get; set; }
        public string? ServiceType { get; set; }
        public bool IsActive { get; set; }

        // NUEVO: cuántas veces fue solicitado en citas
        public int RequestsCount { get; set; }
    }
}
