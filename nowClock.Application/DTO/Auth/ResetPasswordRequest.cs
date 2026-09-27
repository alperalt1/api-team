namespace nowClock.Application.DTO.Auth
{
    public class ResetPasswordRequest
    {
        public string CedulaOrEmail { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
