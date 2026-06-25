using Brittany_Salon_Backend.Application.DTOs.Review;

namespace Brittany_Salon_Backend.Application.Services.Interfaces
{
    public interface IReviewService
    {
        Task<List<ReviewReadDto>> GetAllAsync();
        Task<ReviewReadDto?> GetByIdAsync(int id);
        Task<List<ReviewReadDto>> GetByClientIdAsync(int clientId);
        Task<ReviewReadDto?> GetByClientAndReviewIdAsync(int clientId, int reviewId);
        Task<List<ReviewReadDto>> GetByEmployeeIdAsync(int employeeId);
        Task<ReviewReadDto> CreateAsync(ReviewCreateDto dto);
        Task<ReviewReadDto?> AddResponseAsync(int reviewId, ReviewResponseDto dto);
        Task<bool> DeleteResponseAsync(int reviewId);
        Task<bool> UpdateAsync(int id, int clientId, ReviewUpdateDto dto);
        Task<bool> DeleteAsync(int id, int clientId);
        Task<double> GetAverageRatingByEmployeeAsync(int employeeId);
    }
}
