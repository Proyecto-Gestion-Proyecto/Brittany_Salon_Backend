using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Brittany_Salon_Backend.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;

        private const string PublicFolder = "public";
        private const int MaxImageWidth = 800;
        private const int MaxImageHeight = 800;
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        // Extensiones permitidas (mas confiable que ContentType)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> ProcessAndSaveImageAsync(IFormFile imageFile, string category, int entityId)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("El archivo de imagen no es valido.");

            if (imageFile.Length > MaxBytes)
                throw new ArgumentException("La imagen excede el tamano permitido (5MB).");

            // Validar por extension (mas confiable que ContentType)
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"Formato no permitido. Use: {string.Join(", ", AllowedExtensions)}");

            // category ej: imageUser, imageService, etc.
            category = category.Trim().Trim('/');

            var categoryDirectory = Path.Combine(_environment.ContentRootPath, PublicFolder, category);
            if (!Directory.Exists(categoryDirectory))
                Directory.CreateDirectory(categoryDirectory);

            var fileName = $"{category}_{entityId}_{Guid.NewGuid():N}.webp";
            var filePath = Path.Combine(categoryDirectory, fileName);

            try
            {
                using var inputStream = imageFile.OpenReadStream();
                using var image = await Image.LoadAsync(inputStream);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxImageWidth, MaxImageHeight),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(filePath, new WebpEncoder { Quality = 75 });

                // URL publica
                return $"/{category}/{fileName}";
            }
            catch (OutOfMemoryException)
            {
                throw new ArgumentException("La imagen es demasiado grande para procesar. Intente con una imagen mas pequena.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al procesar la imagen.", ex);
            }
        }

        public bool DeleteImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            try
            {
                // Ej: /imageUser/xxx.webp
                var clean = imageUrl.Split('?')[0].Trim();
                clean = clean.TrimStart('/');

                var filePath = Path.Combine(_environment.ContentRootPath, PublicFolder, clean);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
