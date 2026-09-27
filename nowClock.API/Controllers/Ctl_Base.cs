using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace nowClock.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class Ctl_Base : ControllerBase
    {
        // Parámetros de la URL
        protected Dictionary<string, string> parametros => Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

        // Cabeceras HTTP
        protected Dictionary<string, string> HeaderParams => Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        // Diccionario manual
        protected Dictionary<string, string> parametro = new Dictionary<string, string>();

        protected string? CurrentUserEmail =>
            User.FindFirst(ClaimTypes.Email)?.Value ??
            User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

        protected string? CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        protected string? CurrentUserName =>
            User.FindFirst(ClaimTypes.Name)?.Value ??
            User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ??
            User.FindFirst("nombre")?.Value;

        protected string? CurrentUserIdentificacion =>
            User.FindFirst("identificacion")?.Value ??
            User.FindFirst("cedula")?.Value;

        protected string? CurrentUserCedula => CurrentUserIdentificacion;
    }
}
