using Brittany_Salon_Backend.Application.DTOs.Client;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static partial class ClientValidator
    {
        // Tama�o m�ximo de imagen: 5MB
        private const long MaxImageSize = 5 * 1024 * 1024;

        // Extensiones de imagen permitidas
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

        // Content types permitidos
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

        /// <summary>
        /// Valida todos los campos del DTO de creaci�n de cliente
        /// </summary>
        public static List<string> ValidateCreate(ClientCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validaci�n de cliente...");

            // Validar Nombre
            errors.AddRange(ValidateName(dto.Name));

            // Validar Email
            errors.AddRange(ValidateEmail(dto.Email));

            // Validar Tel�fono (opcional)
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                errors.AddRange(ValidatePhone(dto.Phone));
            }

            // Validar Contrase�a
            errors.AddRange(ValidatePassword(dto.Password));

            // Validar Imagen (si se proporciona)
            if (dto.Image != null)
            {
                errors.AddRange(ValidateImageFile(dto.Image, logger));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validaci�n fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validaci�n exitosa para cliente: {Email}", dto.Email);
            }

            return errors;
        }

        /// <summary>
        /// Valida nombre
        /// </summary>
        private static List<string> ValidateName(string name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("El nombre es obligatorio.");
                return errors;
            }

            var trimmed = name.Trim();

            if (trimmed.Length < 2)
                errors.Add("El nombre debe tener al menos 2 caracteres.");

            if (trimmed.Length > 100)
                errors.Add("El nombre no puede exceder 100 caracteres.");

            // Solo letras, espacios y caracteres especiales comunes
            if (!Regex.IsMatch(trimmed, @"^[a-zA-Z������������\s\-']+$"))
                errors.Add("El nombre solo puede contener letras, espacios, guiones y ap�strofes.");

            return errors;
        }

        /// <summary>
        /// Valida email
        /// </summary>
        private static List<string> ValidateEmail(string email)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("El correo electr�nico es obligatorio.");
                return errors;
            }

            var trimmed = email.Trim();

            if (trimmed.Length > 150)
                errors.Add("El correo electr�nico no puede exceder 150 caracteres.");

            // Validaci�n b�sica de formato de email
            if (!Regex.IsMatch(trimmed, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                errors.Add("El formato del correo electr�nico no es v�lido.");

            return errors;
        }

        /// <summary>
        /// Valida tel�fono (opcional)
        /// </summary>
        private static List<string> ValidatePhone(string phone)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(phone))
                return errors; // Tel�fono es opcional

            var trimmed = phone.Trim();

            if (trimmed.Length > 20)
                errors.Add("El tel�fono no puede exceder 20 caracteres.");

            // Solo d�gitos, espacios, guiones, par�ntesis
            if (!Regex.IsMatch(trimmed, @"^[\d\s\-\(\)\+]+$"))
                errors.Add("El tel�fono solo puede contener d�gitos, espacios, guiones, par�ntesis y el s�mbolo +.");

            return errors;
        }

        /// <summary>
        /// Valida contrase�a
        /// </summary>
        private static List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("La contrase�a es obligatoria.");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("La contrase�a debe tener al menos 8 caracteres.");

            if (password.Length > 255)
                errors.Add("La contrase�a no puede exceder 255 caracteres.");

            // Validar complejidad
            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("La contrase�a debe contener al menos una letra may�scula.");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("La contrase�a debe contener al menos una letra min�scula.");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("La contrase�a debe contener al menos un n�mero.");

            if (!Regex.IsMatch(password, @"[\W_]"))
                errors.Add("La contrase�a debe contener al menos un car�cter especial (ej. !@#$%^&*).");

            return errors;
        }

        /// <summary>
        /// Valida archivo de imagen
        /// </summary>
        private static List<string> ValidateImageFile(IFormFile image, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Validando archivo de imagen: {FileName}, Tama�o: {Size} bytes", image.FileName, image.Length);

            // Validar tama�o
            if (image.Length > MaxImageSize)
            {
                errors.Add("La imagen no puede exceder 5MB.");
                return errors;
            }

            // Validar extensi�n
            var extension = Path.GetExtension(image.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
            {
                errors.Add("Formato de imagen no v�lido. Formatos permitidos: JPEG, PNG, GIF, WebP.");
                return errors;
            }

            // Validar content type
            if (!AllowedContentTypes.Contains(image.ContentType.ToLower()))
            {
                errors.Add("Tipo de contenido de imagen no v�lido.");
                return errors;
            }

            logger?.LogDebug("Archivo de imagen v�lido: {FileName}", image.FileName);
            return errors;
        }

        /// <summary>
        /// Valida los campos del DTO de actualizaci�n de cliente
        /// Solo valida los campos que se proporcionan (no null)
        /// </summary>
        public static List<string> ValidateUpdate(ClientUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validaci�n de actualizaci�n de cliente...");

            // Validar Nombre (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Name))
                errors.AddRange(ValidateName(dto.Name));

            // Validar Email (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Email))
                errors.AddRange(ValidateEmail(dto.Email));

            // Validar Tel�fono (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Phone))
                errors.AddRange(ValidatePhone(dto.Phone));

            // Validar Contrase�a (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                errors.AddRange(ValidatePassword(dto.Password));

            }

            // Validar Imagen (si se proporciona)
            if (dto.Image != null)
            {
                errors.AddRange(ValidateImageFile(dto.Image, logger));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validaci�n de update fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }

            return errors;
        }
    }
}