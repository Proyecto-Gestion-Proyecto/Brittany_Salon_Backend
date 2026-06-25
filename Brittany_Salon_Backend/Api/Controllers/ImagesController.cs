using Brittany_Salon_Backend.Application.DTOs.Image;
using Brittany_Salon_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Brittany_Salon_Backend.Controllers
{
    [ApiController]
    [Route("api/images")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        // POST: /api/images/{category}/{id}
        // form-data: Image (File)
        [HttpPost("{category}/{id:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)] // 10MB (extra margen)
        public async Task<IActionResult> Upload(string category, int id, [FromForm] ImageUploadRequest request)
        {
            var url = await _imageService.ProcessAndSaveImageAsync(request.Image, category, id);
            return Ok(new { url });
        }

        // DELETE: /api/images?url=/imageUser/xxx.webp
        [HttpDelete]
        public IActionResult Delete([FromQuery] string url)
        {
            var ok = _imageService.DeleteImage(url);
            return ok ? Ok() : NotFound();
        }
    }
}
