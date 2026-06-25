using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Brittany_Salon_Backend.Application.DTOs.Product
{
    public class ProductUpdateDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre del producto debe tener al menos 2 caracteres.")]
        [MaxLength(100, ErrorMessage = "El nombre del producto no puede exceder 100 caracteres.")]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres.")]
        public string? ProductDescription { get; set; }

        [Required(ErrorMessage = "El precio del producto es obligatorio.")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0 y no exceder 999999.99.")]
        public decimal Price { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [Required(ErrorMessage = "La categoría del producto es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La categoría seleccionada no es válida.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "El estado del producto es obligatorio.")]
        public bool IsActive { get; set; }

        public IFormFile? Image { get; set; }
    }
}
