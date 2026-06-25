namespace Brittany_Salon_Backend.Infrastructure.Logging
{
    /// <summary>
    /// Interface para el servicio de logging de desarrollo
    /// </summary>
    public interface IDevLogger
    {
        /// <summary>
        /// Log de información general
        /// </summary>
        void LogInfo(string message, params object[] args);

        /// <summary>
        /// Log de advertencia
        /// </summary>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Log de error
        /// </summary>
        void LogError(string message, Exception? exception = null, params object[] args);

        /// <summary>
        /// Log de debug (más detallado)
        /// </summary>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Log de validación con detalles del objeto
        /// </summary>
        void LogValidation(string context, object? data);

        /// <summary>
        /// Indica si el logging está habilitado
        /// </summary>
        bool IsEnabled { get; }
    }
}
