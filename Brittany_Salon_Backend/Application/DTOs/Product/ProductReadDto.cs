using System;

namespace Brittany_Salon_Backend.Application.DTOs.Product
{
    public class ProductReadDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? ProductDescription { get; set; }

        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public bool IsActive { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;
    }
}
