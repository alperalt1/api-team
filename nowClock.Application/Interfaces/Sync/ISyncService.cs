using nowClock.Application.DTO.Auth;
using nowClock.Application.DTO.Sync;
using nowClock.Application.Wrappers;

namespace nowClock.Application.Interfaces.Sync;

public interface ISyncService
{
    Task<ApiResponse<SincronizarResponse>> Sincronizar();
}