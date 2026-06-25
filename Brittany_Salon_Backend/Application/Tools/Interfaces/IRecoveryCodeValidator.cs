using Brittany_Salon_Backend.Application.Tools.Models;

namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryCodeValidator
{
    RecoveryValidationResult Validate(string email, string providedCode);
    void RegisterAttempt(string email, bool success);
    void Invalidate(string email);
}
