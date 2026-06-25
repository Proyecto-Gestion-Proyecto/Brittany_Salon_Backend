namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class ChangeStatusRequestDto
    {
        public string NewStatus { get; set; } = string.Empty;

        public static readonly string[] ValidStatuses = new[]
        {
            "Pendiente",
            "Confirmada",
            "Completada con saldo pendiente",
            "Finalizada",
            "Cancelada"
        };

        public bool IsValid() => ValidStatuses.Contains(NewStatus);
    }
}
