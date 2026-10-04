namespace nowClock.Infrastructure.Services.Mail;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FromName { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Encryption { get; set; } = "tls";
}