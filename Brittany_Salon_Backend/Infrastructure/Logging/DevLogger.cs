using System.Text.Json;

namespace Brittany_Salon_Backend.Infrastructure.Logging
{
    /// <summary>
    /// Implementacion del logger para desarrollo
    /// Solo loggea cuando esta en ambiente de desarrollo
    /// </summary>
    public class DevLogger : IDevLogger
    {
        private readonly ILogger<DevLogger> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly bool _isEnabled;

        public bool IsEnabled => _isEnabled;

        public DevLogger(ILogger<DevLogger> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            _isEnabled = _environment.IsDevelopment();
        }

        public void LogInfo(string message, params object[] args)
        {
            if (!_isEnabled) return;
            _logger.LogInformation($"[DEV] {message}", args);
        }

        public void LogWarning(string message, params object[] args)
        {
            if (!_isEnabled) return;
            _logger.LogWarning($"[DEV][WARN] {message}", args);
        }

        public void LogError(string message, Exception? exception = null, params object[] args)
        {
            if (!_isEnabled) return;
            
            if (exception != null)
                _logger.LogError(exception, $"[DEV][ERROR] {message}", args);
            else
                _logger.LogError($"[DEV][ERROR] {message}", args);
        }

        public void LogDebug(string message, params object[] args)
        {
            if (!_isEnabled) return;
            _logger.LogDebug($"[DEV][DEBUG] {message}", args);
        }

        public void LogValidation(string context, object? data)
        {
            if (!_isEnabled) return;

            try
            {
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string dataJson = data != null 
                    ? JsonSerializer.Serialize(data, jsonOptions) 
                    : "null";

                _logger.LogInformation(
                    "[DEV][VALIDATION] [{Context}]:\n{Data}", 
                    context, 
                    dataJson
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "[DEV] No se pudo serializar datos de validacion: {Error}", 
                    ex.Message
                );
            }
        }
    }
}
