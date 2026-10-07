using System;
using Microsoft.AspNetCore.Identity;

namespace nowClock.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string Cedula { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? FechaNacimiento { get; set; }
        public string? Direccion { get; set; }
        public string? RefreshToken { get; set; }
        public UserAccess Access { get; set; } = new();
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? Preferred2FAProvider { get; set; }
    }
}
