namespace Brittany_Salon_Backend.Application.Tools.Interfaces;

public interface IRecoveryCodeSender
{
    Task SendAsync(string toEmail, string toName, string code, CancellationToken cancellationToken = default);
}
