using Brittany_Salon_Backend.Application.DTOs.Appointment;
using System.Threading.Tasks;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<int> CreateAsync(AppointmentCreateDto dto);
        Task<List<AppointmentReadDto>> GetAllAsync();
        Task<List<AppointmentReadDto>> GetByDateAsync(DateTime date);
        Task<List<AppointmentReadDto>> GetByStatusAsync(string status);
        Task<List<AppointmentReadDto>> GetByClientIdAsync(int clientId);
        Task<bool> UpdatePendingAsync(int appointmentId, AppointmentUpdateDto dto);
        Task<AppointmentDetailDto?> GetByIdAsync(int id);
        Task<bool> CancelAsync(int appointmentId);
        Task<bool> CompleteAsync(int appointmentId, IInventoryService? inventoryService = null);
        Task<AppointmentAvailabilityResponseDto> ValidateAvailabilityAsync(AppointmentAvailabilityRequestDto dto);
        Task<decimal> GetPendingBalanceAsync(int appointmentId);
        Task<decimal> GetPendingBalanceClientAsync(int appointmentId);
        Task<bool> ChangeStatusAsync(int appointmentId, string newStatus, IInventoryService? inventoryService = null);
    }
}
