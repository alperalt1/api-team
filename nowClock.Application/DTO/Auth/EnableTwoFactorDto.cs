using System.ComponentModel.DataAnnotations;

namespace nowClock.Application.DTO.Auth
{
    public class EnableTwoFactorDto
    {
        /// <summary>
        /// true para habilitar 2FA, false para deshabilitarlo. Por defecto true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Proveedor deseado al habilitar: 'Email', 'Phone' o 'Authenticator'. Opcional.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Código de verificación obligatorio (recibido por Email, SMS o app Authenticator).
        /// </summary>
        [Required]
        public string Code { get; set; } = string.Empty;

        public string? Email { get; set; }
    }
}
