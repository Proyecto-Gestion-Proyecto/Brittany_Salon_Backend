using Microsoft.AspNetCore.Http;

namespace Brittany_Salon_Backend.Application.DTOs.Image
{
    public class ImageUploadRequest
    {
        public IFormFile Image { get; set; } = default!;
    }
}
