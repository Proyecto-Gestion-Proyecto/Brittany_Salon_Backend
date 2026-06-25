using Brittany_Salon_Backend.Application.DTOs.Employee;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Brittany_Salon_Backend.Application.Validators
{
    public static partial class EmployeeValidator
    {
        // Tamaño máximo de imagen: 5MB
        private const long MaxImageSize = 5 * 1024 * 1024;

        // Extensiones de imagen permitidas
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

        // Content types permitidos
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

        /// <summary>
        /// Valida todos los campos del DTO de creación de empleado
        /// </summary>
        public static List<string> ValidateCreate(EmployeeCreateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validación de empleado...");

            // Validar Nombre
            errors.AddRange(ValidateName(dto.Name));

            // Validar Email
            errors.AddRange(ValidateEmail(dto.Email));

            // Validar Teléfono
            errors.AddRange(ValidatePhone(dto.Phone));

            // Validar Contraseña
            errors.AddRange(ValidatePassword(dto.Password));

            // Validar Especialidad
            errors.AddRange(ValidateSpecialty(dto.Specialty));

            // Validar Imagen (si se proporciona)
            if (dto.Image != null)
            {
                errors.AddRange(ValidateImageFile(dto.Image, logger));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validacion fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validacion exitosa para empleado: {Email}", dto.Email);
            }

            return errors;
        }

        /// <summary>
        /// Valida los campos del DTO de actualizacion de empleado
        /// Solo valida los campos que se proporcionan (no null)
        /// </summary>
        public static List<string> ValidateUpdate(EmployeeUpdateDto dto, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            logger?.LogDebug("Iniciando validacion de actualizacion de empleado...");

            // Validar Nombre (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Name))
                errors.AddRange(ValidateName(dto.Name));

            // Validar Email (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Email))
                errors.AddRange(ValidateEmail(dto.Email));

            // Validar Telefono (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Phone))
                errors.AddRange(ValidatePhone(dto.Phone));

            // Validar Contrasena (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Password))
                errors.AddRange(ValidatePassword(dto.Password));

            // Validar Especialidad (si se proporciona)
            if (!string.IsNullOrWhiteSpace(dto.Specialty))
                errors.AddRange(ValidateSpecialty(dto.Specialty));

            // Validar Imagen (si se proporciona)
            if (dto.Image != null)
            {
                errors.AddRange(ValidateImageFile(dto.Image, logger));
            }

            if (errors.Count > 0)
            {
                logger?.LogWarning("Validacion de update fallida con {Count} errores: {Errors}", errors.Count, string.Join(", ", errors));
            }
            else
            {
                logger?.LogInfo("Validacion de update exitosa");
            }

            return errors;
        }

        /// <summary>
        /// Valida el nombre del empleado
        /// </summary>
        public static List<string> ValidateName(string? name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("El nombre es obligatorio.");
                return errors;
            }

            var trimmedName = name.Trim();

            if (trimmedName.Length < 2)
                errors.Add("El nombre debe tener al menos 2 caracteres.");

            if (trimmedName.Length > 100)
                errors.Add("El nombre no puede exceder 100 caracteres.");

            // Solo letras, espacios y caracteres especiales comunes en nombres
            if (!NameRegex().IsMatch(trimmedName))
                errors.Add("El nombre solo puede contener letras, espacios, apóstrofes y guiones.");

            // No permitir múltiples espacios consecutivos
            if (trimmedName.Contains("  "))
                errors.Add("El nombre no puede contener espacios múltiples consecutivos.");

            return errors;
        }

        /// <summary>
        /// Valida el correo electrónico
        /// </summary>
        public static List<string> ValidateEmail(string? email)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("El correo electrónico es obligatorio.");
                return errors;
            }

            var trimmedEmail = email.Trim().ToLower();

            if (trimmedEmail.Length > 150)
                errors.Add("El correo electrónico no puede exceder 150 caracteres.");

            // Validar formato de email
            if (!EmailRegex().IsMatch(trimmedEmail))
                errors.Add("El formato del correo electrónico no es válido.");

            // Validar dominios no permitidos (temporales)
            string[] blockedDomains = ["tempmail.com", "throwaway.com", "mailinator.com", "guerrillamail.com"];
            var domain = trimmedEmail.Split('@').LastOrDefault();
            if (domain != null && blockedDomains.Contains(domain))
                errors.Add("No se permiten correos electrónicos temporales.");

            return errors;
        }

        /// <summary>
        /// Valida el número de teléfono
        /// </summary>
        public static List<string> ValidatePhone(string? phone)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add("El numero de telefono es obligatorio.");
                return errors;
            }

            var phoneString = phone.Trim();

            // Telefono de Costa Rica: 8 digitos
            if (phoneString.Length != 8 || !phoneString.All(char.IsDigit))
            {
                errors.Add("El numero de telefono debe tener exactamente 8 digitos.");
                return errors;
            }

            // Debe comenzar con 2, 4, 5, 6, 7 u 8 (prefijos validos en CR)
            char firstDigit = phoneString[0];
            char[] validPrefixes = ['2', '4', '5', '6', '7', '8'];
            if (!validPrefixes.Contains(firstDigit))
                errors.Add("El numero de telefono debe comenzar con 2, 4, 5, 6, 7 u 8.");

            return errors;
        }

        /// <summary>
        /// Valida la contraseña
        /// </summary>
        public static List<string> ValidatePassword(string? password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("La contraseña es obligatoria.");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("La contraseña debe tener al menos 8 caracteres.");

            if (password.Length > 255)
                errors.Add("La contraseña no puede exceder 255 caracteres.");

            if (!password.Any(char.IsUpper))
                errors.Add("La contraseña debe contener al menos una letra mayúscula.");

            if (!password.Any(char.IsLower))
                errors.Add("La contraseña debe contener al menos una letra minúscula.");

            if (!password.Any(char.IsDigit))
                errors.Add("La contraseña debe contener al menos un número.");

            if (!SpecialCharRegex().IsMatch(password))
                errors.Add("La contraseña debe contener al menos un carácter especial (!@#$%^&*(),.?\":{}|<>).");

            // No permitir espacios
            if (password.Contains(' '))
                errors.Add("La contraseña no puede contener espacios.");

            return errors;
        }

        /// <summary>
        /// Valida la especialidad
        /// </summary>
        public static List<string> ValidateSpecialty(string? specialty)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(specialty))
                return errors; // Especialidad es opcional

            var trimmedSpecialty = specialty.Trim();

            if (trimmedSpecialty.Length < 3)
                errors.Add("La especialidad debe tener al menos 3 caracteres.");

            if (trimmedSpecialty.Length > 100)
                errors.Add("La especialidad no puede exceder 100 caracteres.");

            // Solo letras, números, espacios y caracteres comunes
            if (!SpecialtyRegex().IsMatch(trimmedSpecialty))
                errors.Add("La especialidad contiene caracteres no permitidos.");

            return errors;
        }

        /// <summary>
        /// Valida el archivo de imagen (IFormFile)
        /// </summary>
        public static List<string> ValidateImageFile(IFormFile? file, IDevLogger? logger = null)
        {
            var errors = new List<string>();

            if (file == null || file.Length == 0)
                return errors; // Imagen es opcional

            // LOG: Informacion detallada del archivo recibido
            logger?.LogDebug("========== VALIDACION DE IMAGEN ==========");
            logger?.LogDebug("FileName: {FileName}", file.FileName);
            logger?.LogDebug("ContentType recibido: '{ContentType}'", file.ContentType);
            logger?.LogDebug("Tamano: {Size} bytes ({SizeMB:F2} MB)", file.Length, file.Length / 1024.0 / 1024.0);

            // Validar tamano
            if (file.Length > MaxImageSize)
            {
                logger?.LogWarning("Imagen excede tamano maximo: {Size} > {Max}", file.Length, MaxImageSize);
                errors.Add("La imagen no puede exceder 5MB.");
            }

            // Validar extension (MAS CONFIABLE que ContentType)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            logger?.LogDebug("Extension extraida: '{Extension}'", extension);
            
            bool isValidExtension = AllowedExtensions.Contains(extension);
            
            if (!isValidExtension)
            {
                logger?.LogWarning("Extension no permitida: '{Extension}'", extension);
                errors.Add($"Extension de archivo no permitida. Formatos validos: {string.Join(", ", AllowedExtensions)}");
            }
            else
            {
                logger?.LogInfo("Extension valida: '{Extension}'", extension);
            }

            // ContentType: Solo loguear, NO bloquear si la extension es valida
            // Algunos clientes (Bruno, Postman, etc.) envian ContentType incorrecto
            var contentType = file.ContentType.ToLowerInvariant();
            bool isValidContentType = AllowedContentTypes.Contains(contentType) 
                || contentType.StartsWith("image/")
                || contentType == "application/octet-stream";
            
            if (!isValidContentType && isValidExtension)
            {
                // Extension valida pero ContentType raro - aceptamos pero logueamos advertencia
                logger?.LogWarning("ContentType '{ContentType}' no estandar, pero extension '{Extension}' es valida. Aceptado.", contentType, extension);
            }
            else if (!isValidContentType && !isValidExtension)
            {
                // Ambos invalidos - ya se agrego error por extension
                logger?.LogWarning("ContentType y extension invalidos");
            }
            else
            {
                logger?.LogInfo("Imagen valida: extension='{Extension}', contentType='{ContentType}'", extension, contentType);
            }

            logger?.LogDebug("========== FIN VALIDACION DE IMAGEN ==========");

            return errors;
        }

        // Expresiones regulares compiladas para mejor rendimiento
        [GeneratedRegex(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")]
        private static partial Regex NameRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"[!@#$%^&*(),.?""':{}|<>]")]
        private static partial Regex SpecialCharRegex();

        [GeneratedRegex(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\-,\.]+$")]
        private static partial Regex SpecialtyRegex();
    }
}
