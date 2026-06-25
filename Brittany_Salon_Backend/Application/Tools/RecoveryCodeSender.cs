using System.Text;
using Brittany_Salon_Backend.Application.Settings;
using Brittany_Salon_Backend.Application.Tools.Interfaces;
using Brittany_Salon_Backend.Infrastructure.Logging;
using Brittany_Salon_Backend.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Resend;

namespace Brittany_Salon_Backend.Application.Tools;

public sealed class RecoveryCodeSender : IRecoveryCodeSender
{
    private readonly IResend _resend;
    private readonly ResendSettings _resendSettings;
    private readonly RecoveryCodeSettings _recoverySettings;
    private readonly IDevLogger _logger;

    public RecoveryCodeSender(
        IResend resend,
        IOptions<ResendSettings> resendSettings,
        IOptions<RecoveryCodeSettings> recoverySettings,
        IDevLogger logger)
    {
        _resend = resend;
        _resendSettings = resendSettings.Value;
        _recoverySettings = recoverySettings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string toName,
        string code,
        CancellationToken cancellationToken = default)
    {
        var lifetimeMinutes = _recoverySettings.CodeLifetimeMinutes;

        var body = new StringBuilder()
            .Append("Hola ").Append(toName).Append(",\n\n")
            .Append("Tu código de recuperación de contraseña es:\n\n")
            .Append("    ").Append(code).Append("\n\n")
            .Append("Este código es válido durante ")
            .Append(lifetimeMinutes).Append(" minutos.\n\n")
            .Append("Si no solicitaste este código, ignora este mensaje.\n\n")
            .Append("Brittany Salon")
            .ToString();

        var message = new EmailMessage
        {
            From = $"{_resendSettings.SenderName} <{_resendSettings.SenderEmail}>",
            Subject = "Código de recuperación - Brittany Salon",
            TextBody = body
        };

        message.To.Add(toEmail);

        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInfo($"Código de recuperación enviado a: {toEmail}");
    }
}
