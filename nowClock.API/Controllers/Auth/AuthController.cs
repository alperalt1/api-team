using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nowClock.Application.DTO.Auth;
using nowClock.Application.Interfaces.Auth;
using nowClock.Application.Wrappers;
using System.Threading.Tasks;
using ResetPasswordRequest = nowClock.Application.DTO.Auth.ResetPasswordRequest;

namespace nowClock.API.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Ctl_Base
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EndpointSummary("Registrar nuevo usuario")]
        [EndpointDescription("Crea una nueva cuenta de usuario en el sistema con correo electrónico y contraseña. Al registrarse exitosamente, genera y retorna de inmediato el token de acceso JWT y el Refresh Token.")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto req)
        {
            var response = await _authService.RegisterAsync(req);
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("login")]
        [EndpointSummary("Iniciar sesión")]
        [EndpointDescription("Autentica al usuario validando sus credenciales (Email y Password) con protección contra ataques de fuerza bruta. Si tiene 2FA habilitado, retorna un TwoFactorToken temporal para el segundo paso; si no, entrega los tokens definitivos.")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto req)
        {
            var response = await _authService.LoginAsync(req);

            if (!response.Succeeded)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        [EndpointSummary("Renovar token de acceso")]
        [EndpointDescription("Permite obtener un nuevo token JWT válido enviando el token de acceso expirado junto con un Refresh Token válido y no vencido.")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest req)
        {
            var response = await _authService.RefreshTokenAsync(req);

            if (!response.Succeeded) return Unauthorized(response);

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        [EndpointSummary("Solicitar recuperación de contraseña")]
        [EndpointDescription("Genera un token seguro para el restablecimiento de la contraseña asociado al correo electrónico indicado, enviándolo al canal seguro sin exponerlo en la respuesta HTTP.")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            var response = await _authService.ForgotPasswordAsync(req.CedulaOrEmail);

            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("reset-password")]
        [EndpointSummary("Restablecer contraseña con token")]
        [EndpointDescription("Actualiza la contraseña del usuario validando el token de restablecimiento previamente generado e invalidando tokens anteriores.")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var response = await _authService.ResetPasswordAsync(req);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        [EndpointSummary("Cerrar sesión (Revocar tokens)")]
        [EndpointDescription("Invalida y revoca el Refresh Token del usuario autenticado en la base de datos para impedir que se sigan emitiendo nuevos tokens.")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var email = CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new ApiResponse<bool>("Usuario no identificado"));
            }

            var response = await _authService.LogoutAsync(email);
            return Ok(response);
        }

        [HttpPost("verify-2fa")]
        [EndpointSummary("Verificar código 2FA en inicio de sesión")]
        [EndpointDescription("Valida el código TOTP de 6 dígitos junto con el TwoFactorToken temporal emitido durante el login para completar la autenticación.")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto req)
        {
            var response = await _authService.VerifyTwoFactorLoginAsync(req);
            if (!response.Succeeded) return Unauthorized(response);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("2fa-status")]
        [EndpointSummary("Consultar estado actual de 2FA")]
        [EndpointDescription("Requiere Bearer Token. Devuelve si el usuario tiene el 2FA activado, cuál es el método preferido actual (Email, Phone, Authenticator) y qué medios de contacto tiene disponibles.")]
        [ProducesResponseType(typeof(ApiResponse<TwoFactorStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTwoFactorStatus()
        {
            var identifier = CurrentUserCedula ?? CurrentUserId ?? CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Unauthorized(new ApiResponse<TwoFactorStatusResponse>("Usuario no identificado"));
            }

            var response = await _authService.GetTwoFactorStatusAsync(identifier);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("2fa-setup")]
        [EndpointSummary("Obtener clave y código QR para Authenticator")]
        [EndpointDescription("Requiere Bearer Token. Genera la clave y QR exclusivamente para Google/Microsoft Authenticator. Recomendado: Usar 'POST /api/auth/2fa-send-code' con provider 'Authenticator'.")]
        [ProducesResponseType(typeof(ApiResponse<TwoFactorSetupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTwoFactorSetup()
        {
            var identifier = CurrentUserCedula ?? CurrentUserId ?? CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Unauthorized(new ApiResponse<TwoFactorSetupResponse>("Usuario no identificado"));
            }

            var response = await _authService.GetTwoFactorSetupAsync(identifier);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("2fa/toggle")]
        [EndpointSummary("Habilitar o deshabilitar 2FA con validación de código")]
        [EndpointDescription("Requiere Bearer Token. Permite activar ('enable': true) o desactivar ('enable': false) la protección 2FA. En ambos casos valida el código de verificación obligatorio para autorizar la operación.")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto req)
        {
            var identifier = CurrentUserCedula ?? CurrentUserId ?? CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Unauthorized(new ApiResponse<bool>("Usuario no identificado"));
            }
            req.Email = identifier;
    
            var response = await _authService.EnableTwoFactorAsync(req);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("2fa-send-code")]
        [EndpointSummary("Solicitar código de prueba o QR para método 2FA")]
        [EndpointDescription("Requiere Bearer Token. Envía un código de prueba al canal solicitado ('Email', 'Phone') o devuelve clave y QR ('Authenticator'). Permite al usuario verificar que efectivamente le llegan los códigos antes de activarlo, cambiarlo o desactivarlo.")]
        [ProducesResponseType(typeof(ApiResponse<SendTwoFactorCodeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendTwoFactorCode([FromBody] SendTwoFactorCodeDto req)
        {
            var identifier = CurrentUserCedula ?? CurrentUserId ?? CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Unauthorized(new ApiResponse<SendTwoFactorCodeResponse>("Usuario no identificado"));
            }

            var response = await _authService.SendTwoFactorCodeAsync(identifier, req.Provider);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("2fa-preference")]
        [EndpointSummary("Confirmar código y activar/actualizar método preferido de 2FA")]
        [EndpointDescription("Requiere Bearer Token. Valida el código de prueba recibido. Al comprobar que los códigos sí le llegan al usuario, confirma el medio de contacto y activa el método como el 2FA preferido de la cuenta.")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdatePreference([FromBody] Update2FAPreferenceDto req)
        {
            var identifier = CurrentUserCedula ?? CurrentUserId ?? CurrentUserEmail;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Unauthorized(new ApiResponse<bool>("Usuario no identificado"));
            }

            var response = await _authService.Update2FAPreferenceAsync(identifier, req.Provider, req.Code);
            if (!response.Succeeded) return BadRequest(response);
            return Ok(response);
        }
    }
}
