using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using nowClock.Application.DTO.Auth;
using nowClock.Application.Interfaces.Auth;
using nowClock.Application.Wrappers;
using nowClock.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ResetPasswordRequest = nowClock.Application.DTO.Auth.ResetPasswordRequest;

namespace nowClock.Infrastructure.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthService> _logger;

        #region Constructor
        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterDto req)
        {
            if (string.IsNullOrWhiteSpace(req.Cedula) || string.IsNullOrWhiteSpace(req.Password))
            {
                return new ApiResponse<AuthResponse>("La cédula y la contraseña son obligatorias.");
            }

            var existingByCedula = await _userManager.FindByNameAsync(req.Cedula)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == req.Cedula);

            if (existingByCedula != null)
            {
                return new ApiResponse<AuthResponse>("Ya existe un usuario registrado con esta cédula.");
            }

            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var existingByEmail = await _userManager.FindByEmailAsync(req.Email);
                if (existingByEmail != null)
                {
                    return new ApiResponse<AuthResponse>("Ya existe un usuario registrado con este correo electrónico.");
                }
            }

            var user = new ApplicationUser
            {
                UserName = req.Cedula,
                Cedula = req.Cedula,
                Nombre = req.Nombre,
                Email = req.Email,
                PhoneNumber = req.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<AuthResponse>("No se pudo registrar el usuario.", errors);
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            var authData = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken
            };

            return new ApiResponse<AuthResponse>(authData, "Usuario registrado exitosamente");
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginDto req)
        {
            if (string.IsNullOrWhiteSpace(req.Cedula) || string.IsNullOrWhiteSpace(req.Password))
            {
                return new ApiResponse<AuthResponse>("La cédula y la contraseña son obligatorias.");
            }

            // Buscar usuario por Cédula (UserName)
            var user = await _userManager.FindByNameAsync(req.Cedula)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == req.Cedula);

            if (user == null)
            {
                return new ApiResponse<AuthResponse>("Cédula o contraseña incorrectas.");
            }

            // Verificar si la cuenta se encuentra bloqueada por fuerza bruta
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var minutesRemaining = lockoutEnd.HasValue 
                    ? Math.Max(1, (int)(lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes) 
                    : 15;

                return new ApiResponse<AuthResponse>($"La cuenta está temporalmente bloqueada debido a múltiples intentos fallidos. Intente nuevamente en {minutesRemaining} minutos.");
            }

            // Validar contraseña con lockoutOnFailure = true para protección contra ataques de fuerza bruta
            var isPasswordValid = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);

            if (isPasswordValid.IsLockedOut)
            {
                _logger.LogWarning("Cuenta bloqueada por múltiples intentos fallidos para la cédula: {Cedula}", user.Cedula);
                return new ApiResponse<AuthResponse>("La cuenta ha sido bloqueada temporalmente por exceder el número máximo de intentos fallidos.");
            }

            if (!isPasswordValid.Succeeded)
            {
                return new ApiResponse<AuthResponse>("Cédula o contraseña incorrectas.");
            }

            // Resetear contador de accesos fallidos al superar la contraseña
            await _userManager.ResetAccessFailedCountAsync(user);

            // Flujo con 2FA habilitado
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                var preferredProvider = user.Preferred2FAProvider ?? _userManager.Options.Tokens.AuthenticatorTokenProvider;
                var twoFactorSessionToken = GenerateTwoFactorSessionToken(user);

                return new ApiResponse<AuthResponse>(new AuthResponse
                {
                    RequiresTwoFactor = true,
                    PreferredProvider = preferredProvider,
                    TwoFactorToken = twoFactorSessionToken
                }, "Se requiere autenticación de dos factores para completar el inicio de sesión.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            var authData = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken
            };

            return new ApiResponse<AuthResponse>(authData, "Autenticación exitosa.");
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(TokenRequest req)
        {
            var principal = GetPrincipalFromExpiredToken(req.Token);
            if (principal == null) return new ApiResponse<AuthResponse>("Token de acceso inválido.");

            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) 
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return new ApiResponse<AuthResponse>("Token de acceso inválido.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.RefreshToken != req.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new ApiResponse<AuthResponse>("Refresh Token inválido o expirado. Debe iniciar sesión nuevamente.");
            }

            var newJwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            var refresh = new AuthResponse
            {
                Token = newJwtToken,
                RefreshToken = newRefreshToken
            };

            return new ApiResponse<AuthResponse>(refresh, "Token de acceso renovado exitosamente.");
        }

        public async Task<ApiResponse<string>> ForgotPasswordAsync(string cedulaOrEmail)
        {
            if (string.IsNullOrWhiteSpace(cedulaOrEmail))
            {
                return new ApiResponse<string>("La identificación (cédula) o correo es requerida.");
            }

            var user = await _userManager.FindByNameAsync(cedulaOrEmail)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == cedulaOrEmail)
                ?? await _userManager.FindByEmailAsync(cedulaOrEmail);

            if (user == null)
            {
                // Mensaje genérico para prevenir enumeración de usuarios
                return new ApiResponse<string>("Si el usuario está registrado, se enviarán las instrucciones de recuperación.", "Solicitud procesada");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Se registra en los logs del servidor para trazabilidad y pruebas en desarrollo
            _logger.LogInformation("Token de restablecimiento de contraseña generado para Cédula: {Cedula} / Email: {Email}: {Token}", user.Cedula, user.Email, token);

            return new ApiResponse<string>("Si el usuario está registrado, se enviarán las instrucciones de recuperación al canal configurado.", "Solicitud procesada");
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CedulaOrEmail) || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return new ApiResponse<bool>("Todos los campos (cédula/correo, token y nueva contraseña) son requeridos.");
            }

            var user = await _userManager.FindByNameAsync(req.CedulaOrEmail)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == req.CedulaOrEmail)
                ?? await _userManager.FindByEmailAsync(req.CedulaOrEmail);

            if (user == null) return new ApiResponse<bool>("Usuario no encontrado.");

            var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ApiResponse<bool>("Token inválido o expirado.", errors);
            }

            // Invalidar el refresh token existente para forzar re-autenticación
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            return new ApiResponse<bool>(true, "Contraseña restablecida exitosamente.");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(string emailOrCedula)
        {
            var user = await _userManager.FindByNameAsync(emailOrCedula)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == emailOrCedula)
                ?? await _userManager.FindByEmailAsync(emailOrCedula);

            if (user == null) return new ApiResponse<bool>("Usuario no encontrado.");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            return new ApiResponse<bool>(true, "Sesión cerrada exitosamente.");
        }

        public async Task<ApiResponse<AuthResponse>> VerifyTwoFactorLoginAsync(VerifyTwoFactorDto req)
        {
            ApplicationUser? user = null;

            // Opción 1 (Segura): Validar token de sesión 2FA emitido durante el login previo
            if (!string.IsNullOrWhiteSpace(req.TwoFactorToken))
            {
                var principal = ValidateTwoFactorSessionToken(req.TwoFactorToken);
                if (principal == null)
                {
                    return new ApiResponse<AuthResponse>("La sesión de verificación 2FA ha expirado o es inválida. Inicie sesión nuevamente.");
                }

                var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) 
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    user = await _userManager.FindByIdAsync(userId);
                }
            }
            // Opción 2: Fallback por Cédula si no se utilizó TwoFactorToken
            else if (!string.IsNullOrWhiteSpace(req.Cedula))
            {
                user = await _userManager.FindByNameAsync(req.Cedula)
                    ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == req.Cedula);
            }

            if (user == null)
            {
                return new ApiResponse<AuthResponse>("Usuario no encontrado o sesión 2FA inválida.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new ApiResponse<AuthResponse>("La cuenta se encuentra temporalmente bloqueada por exceso de intentos fallidos.");
            }

            var provider = user.Preferred2FAProvider ?? _userManager.Options.Tokens.AuthenticatorTokenProvider;

            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, provider, req.Code);

            if (!isCodeValid)
            {
                await _userManager.AccessFailedAsync(user);
                return new ApiResponse<AuthResponse>("Código de autenticación inválido.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            var authData = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken
            };

            return new ApiResponse<AuthResponse>(authData, "Autenticación de dos factores exitosa.");
        }

        public async Task<ApiResponse<TwoFactorSetupResponse>> GetTwoFactorSetupAsync(string userIdOrCedula)
        {
            var user = await _userManager.FindByIdAsync(userIdOrCedula)
                ?? await _userManager.FindByNameAsync(userIdOrCedula)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == userIdOrCedula);

            if (user == null) return new ApiResponse<TwoFactorSetupResponse>("Usuario no encontrado.");

            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var appName = _configuration["appName"] ?? "NowClock";
            var identifier = !string.IsNullOrEmpty(user.Cedula) ? user.Cedula : user.UserName ?? "user";
            var qrCodeUri = $"otpauth://totp/{appName}:{identifier}?secret={key}&issuer={appName}";

            var response = new TwoFactorSetupResponse
            {
                SharedKey = key ?? string.Empty,
                AuthenticatorUri = qrCodeUri
            };

            return new ApiResponse<TwoFactorSetupResponse>(response, "Clave de configuración 2FA generada exitosamente.");
        }

        public async Task<ApiResponse<bool>> EnableTwoFactorAsync(EnableTwoFactorDto req)
        {
            if (string.IsNullOrWhiteSpace(req.Email)) // Se usa como identificador general del usuario en sesión
            {
                return new ApiResponse<bool>("El identificador de usuario es requerido.");
            }

            if (!req.Enable)
            {
                return await DisableTwoFactorAsync(req.Email, req.Code);
            }

            var provider = !string.IsNullOrWhiteSpace(req.Provider)
                ? req.Provider
                : _userManager.Options.Tokens.AuthenticatorTokenProvider;

            return await Update2FAPreferenceAsync(req.Email, provider, req.Code);
        }

        public async Task<ApiResponse<SendTwoFactorCodeResponse>> SendTwoFactorCodeAsync(string userIdentifier, string provider)
        {
            var user = await _userManager.FindByIdAsync(userIdentifier)
                ?? await _userManager.FindByNameAsync(userIdentifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == userIdentifier)
                ?? await _userManager.FindByEmailAsync(userIdentifier);

            if (user == null) return new ApiResponse<SendTwoFactorCodeResponse>("Usuario no encontrado.");

            if (string.IsNullOrWhiteSpace(provider) || provider.Equals("Current", StringComparison.OrdinalIgnoreCase) || provider.Equals("Disable", StringComparison.OrdinalIgnoreCase))
            {
                provider = user.Preferred2FAProvider ?? _userManager.Options.Tokens.AuthenticatorTokenProvider;
            }

            var authProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;

            // Opción 1: Aplicación Autenticadora (TOTP)
            if (provider.Equals("Authenticator", StringComparison.OrdinalIgnoreCase) || provider == authProvider)
            {
                var key = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(key))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user);
                    key = await _userManager.GetAuthenticatorKeyAsync(user);
                }

                var appName = _configuration["appName"] ?? "NowClock";
                var identifier = !string.IsNullOrEmpty(user.Cedula) ? user.Cedula : user.UserName ?? "user";
                var qrCodeUri = $"otpauth://totp/{appName}:{identifier}?secret={key}&issuer={appName}";

                var response = new SendTwoFactorCodeResponse
                {
                    Provider = "Authenticator",
                    SharedKey = key,
                    AuthenticatorUri = qrCodeUri
                };

                return new ApiResponse<SendTwoFactorCodeResponse>(response, "Escanee el código QR o agregue la clave secreta en su app autenticadora. Luego ingrese el código de 6 dígitos para confirmar.");
            }

            // Opción 2: Correo Electrónico
            if (provider.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return new ApiResponse<SendTwoFactorCodeResponse>("El usuario no tiene una dirección de correo electrónico registrada para recibir códigos.");
                }

                var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

                // En desarrollo se registra en los logs del servidor
                _logger.LogInformation("Código de verificación 2FA para Email ({Email}): {Code}", user.Email, code);

                var maskedEmail = MaskEmail(user.Email);
                var response = new SendTwoFactorCodeResponse
                {
                    Provider = "Email",
                    Destination = maskedEmail
                };

                return new ApiResponse<SendTwoFactorCodeResponse>(response, $"Se ha enviado un código de verificación a {maskedEmail}. Ingréselo en el siguiente paso para confirmar el cambio.");
            }

            // Opción 3: Teléfono / SMS
            if (provider.Equals("Phone", StringComparison.OrdinalIgnoreCase) || provider.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    return new ApiResponse<SendTwoFactorCodeResponse>("El usuario no tiene un número de teléfono registrado para recibir mensajes SMS.");
                }

                var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Phone");

                // En desarrollo se registra en los logs del servidor
                _logger.LogInformation("Código de verificación 2FA para SMS ({Phone}): {Code}", user.PhoneNumber, code);

                var maskedPhone = MaskPhone(user.PhoneNumber);
                var response = new SendTwoFactorCodeResponse
                {
                    Provider = "Phone",
                    Destination = maskedPhone
                };

                return new ApiResponse<SendTwoFactorCodeResponse>(response, $"Se ha enviado un código de verificación a {maskedPhone}. Ingréselo en el siguiente paso para confirmar el cambio.");
            }

            return new ApiResponse<SendTwoFactorCodeResponse>("Proveedor de autenticación no reconocido. Opciones válidas: 'Authenticator', 'Email', 'Phone'.");
        }

        public async Task<ApiResponse<bool>> Update2FAPreferenceAsync(string userIdentifier, string provider, string code)
        {
            var user = await _userManager.FindByIdAsync(userIdentifier)
                ?? await _userManager.FindByNameAsync(userIdentifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == userIdentifier)
                ?? await _userManager.FindByEmailAsync(userIdentifier);

            if (user == null) return new ApiResponse<bool>("Usuario no encontrado.");

            if (string.IsNullOrWhiteSpace(code))
            {
                return new ApiResponse<bool>("Se requiere el código de verificación del nuevo método para confirmar que tiene acceso a él y que los códigos le llegan correctamente.");
            }

            string normalizedProvider;
            if (provider.Equals("Authenticator", StringComparison.OrdinalIgnoreCase) || provider == _userManager.Options.Tokens.AuthenticatorTokenProvider)
            {
                normalizedProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
            }
            else if (provider.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return new ApiResponse<bool>("El usuario no tiene una dirección de correo registrada para usar este método.");
                }
                normalizedProvider = "Email";
            }
            else if (provider.Equals("Phone", StringComparison.OrdinalIgnoreCase) || provider.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    return new ApiResponse<bool>("El usuario no tiene un número de teléfono registrado para usar este método.");
                }
                normalizedProvider = "Phone";
            }
            else
            {
                return new ApiResponse<bool>("Proveedor no reconocido. Métodos válidos: 'Email', 'Phone', 'Authenticator'.");
            }

            // Validar que el código suministrado realmente funciona con el método solicitado
            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, normalizedProvider, code);
            if (!isCodeValid)
            {
                return new ApiResponse<bool>("El código de verificación no es válido o ha expirado. Verifique que lo haya recibido correctamente e intente de nuevo.");
            }

            // Al haber verificado exitosamente el código que enviamos a su correo o teléfono,
            // confirmamos automáticamente el medio de contacto
            if (normalizedProvider == "Email" && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
            }
            else if (normalizedProvider == "Phone" && !user.PhoneNumberConfirmed)
            {
                user.PhoneNumberConfirmed = true;
            }

            user.Preferred2FAProvider = normalizedProvider;

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
            }

            await _userManager.UpdateAsync(user);

            var friendlyName = normalizedProvider == _userManager.Options.Tokens.AuthenticatorTokenProvider ? "Authenticator" : normalizedProvider;
            return new ApiResponse<bool>(true, $"Método de 2FA '{friendlyName}' verificado y activado exitosamente como preferido.");
        }

        public async Task<ApiResponse<TwoFactorStatusResponse>> GetTwoFactorStatusAsync(string userIdentifier)
        {
            var user = await _userManager.FindByIdAsync(userIdentifier)
                ?? await _userManager.FindByNameAsync(userIdentifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == userIdentifier)
                ?? await _userManager.FindByEmailAsync(userIdentifier);

            if (user == null) return new ApiResponse<TwoFactorStatusResponse>("Usuario no encontrado.");

            var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var preferred = user.Preferred2FAProvider;
            if (preferred == _userManager.Options.Tokens.AuthenticatorTokenProvider)
            {
                preferred = "Authenticator";
            }

            var status = new TwoFactorStatusResponse
            {
                IsTwoFactorEnabled = isEnabled,
                PreferredProvider = isEnabled ? preferred : null,
                HasEmail = !string.IsNullOrWhiteSpace(user.Email),
                MaskedEmail = !string.IsNullOrWhiteSpace(user.Email) ? MaskEmail(user.Email) : null,
                HasPhoneNumber = !string.IsNullOrWhiteSpace(user.PhoneNumber),
                MaskedPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? MaskPhone(user.PhoneNumber) : null
            };

            return new ApiResponse<TwoFactorStatusResponse>(status, "Estado de autenticación de dos factores obtenido correctamente.");
        }

        public async Task<ApiResponse<bool>> DisableTwoFactorAsync(string userIdentifier, string code)
        {
            var user = await _userManager.FindByIdAsync(userIdentifier)
                ?? await _userManager.FindByNameAsync(userIdentifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == userIdentifier)
                ?? await _userManager.FindByEmailAsync(userIdentifier);

            if (user == null) return new ApiResponse<bool>("Usuario no encontrado.");

            var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isEnabled)
            {
                return new ApiResponse<bool>("La autenticación de dos factores ya se encuentra deshabilitada en esta cuenta.");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return new ApiResponse<bool>("Se requiere el código de verificación de su método 2FA actual para confirmar la desactivación.");
            }

            var provider = user.Preferred2FAProvider ?? _userManager.Options.Tokens.AuthenticatorTokenProvider;
            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, provider, code);
            if (!isCodeValid)
            {
                return new ApiResponse<bool>("Código de verificación inválido o expirado. No se realizaron cambios.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            user.Preferred2FAProvider = null;
            await _userManager.UpdateAsync(user);

            return new ApiResponse<bool>(true, "Autenticación de dos factores deshabilitada exitosamente.");
        }
        #endregion

        #region Private Methods
        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["SecretKey"] ?? jwtSettings["Secret"]
                ?? "Default_Development_Super_Secret_Key_32_Bytes_Long!";
            var key = Encoding.UTF8.GetBytes(secretString);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("identificacion", user.Cedula),
                new Claim("cedula", user.Cedula),
                new Claim(JwtRegisteredClaimNames.Name, user.Nombre ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Nombre ?? string.Empty),
                new Claim("nombre", user.Nombre ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateTwoFactorSessionToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["SecretKey"] ?? jwtSettings["Secret"]
                ?? "Default_Development_Super_Secret_Key_32_Bytes_Long!";
            var key = Encoding.UTF8.GetBytes(secretString);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("identificacion", user.Cedula),
                new Claim("cedula", user.Cedula),
                new Claim(ClaimTypes.Name, user.Nombre ?? string.Empty),
                new Claim("purpose", "2fa_verification"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(5), // Vigencia corta para verificar 2FA
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private ClaimsPrincipal? ValidateTwoFactorSessionToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["SecretKey"] ?? jwtSettings["Secret"]
                ?? "Default_Development_Super_Secret_Key_32_Bytes_Long!";

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
                ValidIssuer = jwtSettings["Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretString)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                var purpose = principal.FindFirstValue("purpose");
                if (purpose != "2fa_verification") return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["SecretKey"] ?? jwtSettings["Secret"]
                ?? "Default_Development_Super_Secret_Key_32_Bytes_Long!";

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
                ValidIssuer = jwtSettings["Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretString)),
                ValidateLifetime = false // Para refresh token se permite que el token JWT esté expirado
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token Inválido");
            }

            return principal;
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            var maskedName = name.Length > 2 ? $"{name[0]}***{name[^1]}" : $"{name[0]}*";
            return $"{maskedName}@{parts[1]}";
        }

        private static string MaskPhone(string phone)
        {
            if (phone.Length <= 4) return phone;
            return $"{phone[..3]}****{phone[^2..]}";
        }
        #endregion
    }
}
