namespace nowClock.Application.DTO.Auth
{
    public class RegisterDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
