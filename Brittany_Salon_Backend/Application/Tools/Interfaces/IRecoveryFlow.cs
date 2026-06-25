using Brittany_Salon_Backend.Application.DTOs.Authentication;
using Brittany_Salon_Backend.Application.Tools.Models;

namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryFlow
{
    Task<RecoveryRequestOutcome> RequestCodeAsync(ForgotPasswordRequestDto dto, string clientIp, CancellationToken cancellationToken = default);
    Task<RecoveryResetOutcome> ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken cancellationToken = default);
}
