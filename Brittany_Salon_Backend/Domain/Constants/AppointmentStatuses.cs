namespace Brittany_Salon_Backend.Domain.Constants
{
    public static class AppointmentStatuses
    {
        public const string Pending = "Pendiente";
        public const string Confirmed = "Confirmada";
        public const string CompletedPendingPayment = "Completada con saldo pendiente";
        public const string Finalized = "Finalizada";
        public const string Cancelled = "Cancelada";

        public static readonly string[] All =
        [
            Pending,
            Confirmed,
            CompletedPendingPayment,
            Finalized,
            Cancelled
        ];

        public static bool IsValid(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            All.Contains(status, StringComparer.OrdinalIgnoreCase);

        public static bool IsFinalState(string? status) =>
            string.Equals(status, Finalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

        public static bool CanBeCancelled(string? status) =>
            string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, Confirmed, StringComparison.OrdinalIgnoreCase);

        public static bool CanBeEdited(string? status) =>
            string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, Confirmed, StringComparison.OrdinalIgnoreCase);
    }
}
