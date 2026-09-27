using System;
using Microsoft.AspNetCore.Identity;

namespace nowClock.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string Cedula { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? Preferred2FAProvider { get; set; }
    }
}
