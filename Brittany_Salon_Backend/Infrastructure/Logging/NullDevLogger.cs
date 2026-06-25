namespace Brittany_Salon_Backend.Infrastructure.Logging
{
    /// <summary>
    /// Implementación nula del logger (no hace nada)
    /// Se usa en producción para evitar cualquier overhead
    /// </summary>
    public class NullDevLogger : IDevLogger
    {
        public bool IsEnabled => false;

        public void LogInfo(string message, params object[] args) { }
        public void LogWarning(string message, params object[] args) { }
        public void LogError(string message, Exception? exception = null, params object[] args) { }
        public void LogDebug(string message, params object[] args) { }
        public void LogValidation(string context, object? data) { }
    }
}
