namespace nowClock.Application.DTO.Auth
{
    public class LoginDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientPlatform { get; set; } = string.Empty;
    }
}
