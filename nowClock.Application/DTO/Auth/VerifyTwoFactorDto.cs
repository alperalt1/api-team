namespace nowClock.Application.DTO.Auth
{
    public class VerifyTwoFactorDto
    {
        public string? Cedula { get; set; }
        public string? TwoFactorToken { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
