using nowClock.Application.DTO.Auth;
using nowClock.Application.Wrappers;
using System.Threading.Tasks;

namespace nowClock.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterDto req);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginDto req);
        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(TokenRequest req);
        Task<ApiResponse<string>> ForgotPasswordAsync(string email);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest req);
        Task<ApiResponse<bool>> LogoutAsync(string email);

        // Two-Factor Authentication
        Task<ApiResponse<AuthResponse>> VerifyTwoFactorLoginAsync(VerifyTwoFactorDto req);
        Task<ApiResponse<TwoFactorSetupResponse>> GetTwoFactorSetupAsync(string email);
        Task<ApiResponse<bool>> EnableTwoFactorAsync(EnableTwoFactorDto req);
        Task<ApiResponse<SendTwoFactorCodeResponse>> SendTwoFactorCodeAsync(string userIdentifier, string provider);
        Task<ApiResponse<bool>> Update2FAPreferenceAsync(string userIdentifier, string provider, string code);
        Task<ApiResponse<TwoFactorStatusResponse>> GetTwoFactorStatusAsync(string userIdentifier);
        Task<ApiResponse<bool>> DisableTwoFactorAsync(string userIdentifier, string code);
    }
}
