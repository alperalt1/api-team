using System;

namespace nowClock.Application.DTO.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; } = false;
        public string? PreferredProvider { get; set; }
        public string? TwoFactorToken { get; set; }
    }

    public class Update2FAPreferenceDto
    {
        public string Provider { get; set; } = string.Empty; // "Authenticator", "Email", "Phone"
        public string Code { get; set; } = string.Empty;     // Código de prueba recibido
    }

    public class SendTwoFactorCodeDto
    {
        public string Provider { get; set; } = string.Empty; // "Email", "Phone", "Authenticator"
    }

    public class SendTwoFactorCodeResponse
    {
        public string Provider { get; set; } = string.Empty;
        public string? Destination { get; set; }
        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
    }

    public class TwoFactorStatusResponse
    {
        public bool IsTwoFactorEnabled { get; set; }
        public string? PreferredProvider { get; set; }
        public bool HasEmail { get; set; }
        public string? MaskedEmail { get; set; }
        public bool HasPhoneNumber { get; set; }
        public string? MaskedPhone { get; set; }
    }

    public class DisableTwoFactorDto
    {
        public string Code { get; set; } = string.Empty;
    }
}
