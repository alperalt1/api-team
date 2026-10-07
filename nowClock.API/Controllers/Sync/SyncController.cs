using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nowClock.Application.DTO.Sync;
using nowClock.Application.Interfaces.Sync;
using nowClock.Application.Wrappers;

namespace nowClock.API.Controllers.Sync;

[Route("api/[controller]")]
[ApiController]
public class SyncController: Ctl_Base
{
    private readonly ISyncService _syncService;

    public SyncController(ISyncService syncService)
    {
        _syncService = syncService;
    }
    
    [Authorize]
    [HttpGet("sync")]
    [EndpointSummary("Sincronizar con la hora del servidor")]
    [EndpointDescription("Obtiene la hora oficial del servidor en milisegundos UNIX y formato ISO para sincronizar el reloj de la aplicación.")]
    [ProducesResponseType(typeof(ApiResponse<SincronizarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sincronizar()
    {
        var response = await _syncService.Sincronizar();
        if (!response.Succeeded)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }
}