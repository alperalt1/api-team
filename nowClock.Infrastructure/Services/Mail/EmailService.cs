using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using nowClock.Application.Interfaces.Mail;

namespace nowClock.Infrastructure.Services.Mail;

public class EmailService: IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }
    
    public async Task SendPasswordResetCodeAsync(string toEmail, string code)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Código de Restablecimiento de Contraseña - NowClock";

        var builder = new BodyBuilder
        {
            HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                        <h2>Restablecimiento de Contraseña</h2>
                        <p>Has solicitado restablecer tu contraseña. Utiliza el siguiente código de 6 dígitos para completar el proceso:</p>
                        <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #007bff; border-radius: 5px; margin: 20px 0;'>
                            {code}
                        </div>
                        <p>Este código es de uso personal y expira en pocos minutos.</p>
                        <p style='font-size: 12px; color: #777;'>Si no solicitaste este cambio, puedes ignorar este mensaje de manera segura.</p>
                    </div>"
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        // Determinar socket options según la propiedad Encryption
        var socketOptions = _smtpSettings.Encryption.Equals("tls", StringComparison.OrdinalIgnoreCase) || _smtpSettings.Encryption.Equals("starttls", StringComparison.OrdinalIgnoreCase)
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions);

        if (!string.IsNullOrEmpty(_smtpSettings.UserName))
        {
            await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}