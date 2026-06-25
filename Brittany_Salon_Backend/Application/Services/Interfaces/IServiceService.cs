using Brittany_Salon_Backend.Application.DTOs.Service;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
        public interface IServiceService
        {
            Task<List<ServiceReadDto>> GetAllAsync(bool onlyActive = false);
            Task<ServiceReadDto?> GetByIdAsync(int id);
            Task<ServiceReadDto> CreateAsync(ServiceCreateDto dto);
            Task<bool> UpdateAsync(int id, ServiceUpdateDto dto);
            Task<bool> DeactivateAsync(int id);
            Task<bool> UpdateImageUrlAsync(int serviceId, string imageUrl);
            Task<List<ServiceReadDto>> SearchByNameAsync(string name, bool onlyActive = false);
            Task<bool> DeletePermanentlyAsync(int id);
            Task<bool> ReactivateAsync(int id);
            Task<List<FeaturedServiceReadDto>> GetFeaturedAsync(int top = 5, bool onlyActive = true);



    }
}
