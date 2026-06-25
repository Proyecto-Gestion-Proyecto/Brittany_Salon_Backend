using Microsoft.AspNetCore.Http;

namespace Brittany_Salon_Backend.Infrastructure.Services
{
    public interface IImageService
    {
        /// <summary>
        /// Procesa y guarda una imagen de empleado, convirtiéndola a WebP
        /// </summary>
        /// <param name="imageFile">Archivo de imagen (IFormFile)</param>
        /// <param name="employeeId">ID del empleado para generar nombre único</param>
        /// <returns>URL relativa de la imagen guardada</returns>
        Task<string> ProcessAndSaveImageAsync(IFormFile imageFile, string category, int entityId);
        /// <summary>
        /// Elimina una imagen de empleado del sistema de archivos
        /// </summary>
        /// <param name="imageUrl">URL relativa de la imagen a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        bool DeleteImage(string imageUrl);

   

    }
}
