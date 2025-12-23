using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.Interfaces.IService.Keycloak;

namespace SAMS_BE.Controllers.Keycloak
{
    [ApiController]
    [Route("api/keycloak/roles")]
    public class KeycloakController : ControllerBase
    {
        private readonly IKeycloakRoleService _svc;

        public KeycloakController(IKeycloakRoleService svc) => _svc = svc;


        [HttpGet("client")]
        public async Task<IActionResult> GetClientRoles(CancellationToken ct)
            => Ok(await _svc.GetClientRolesAsync(null, ct));

    }
}
