using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nowClock.Application.DTO.Auth;
using nowClock.Application.DTO.Sync;
using nowClock.Application.Interfaces.Sync;
using nowClock.Application.Wrappers;
using nowClock.Infrastructure.Identity;

namespace nowClock.Infrastructure.Services.Sync;

public class SyncService: ISyncService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncService> _logger;

    #region Constructor
    public SyncService(
        UserManager<ApplicationUser> userManager, 
        IConfiguration configuration,
        ILogger<SyncService> logger
        )
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    public Task<ApiResponse<SincronizarResponse>> Sincronizar()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var serverTime = new SincronizarResponse
        {
            UnixTimestampMs = nowUtc.ToUnixTimeMilliseconds(),
            UtcIso = nowUtc.ToString("o"),
            TimeZone = "America/Guayaquil"
        };
        _logger.LogInformation("Sincronización de hora del servidor realizada. UnixMs: {UnixMs}", serverTime.UnixTimestampMs);
        var response = new ApiResponse<SincronizarResponse>(serverTime, "Hora sincronizada exitosamente.");
        return Task.FromResult(response);
    }
    
    #endregion
}