namespace nowClock.Application.Interfaces.Mail;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string Email, string code);
}