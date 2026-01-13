using Billing.Domain.DTOs.Responses;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ModulesController(IModuleRepository moduleRepository, ILogger<ModulesController> logger) : ControllerBase
{
    private readonly IModuleRepository _moduleRepository = moduleRepository;
    private readonly ILogger<ModulesController> _logger = logger;

    /// <summary>
    /// Lista todos os módulos ativos disponíveis
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ModuleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllModules(CancellationToken ct)
    {
        var modules = await _moduleRepository.GetActiveModulesAsync(ct);

        var response = modules.Select(m => new ModuleResponse(
            m.Id,
            m.Code,
            m.Name,
            m.Description,
            m.PricePerUser,
            m.IsActive,
            m.QuantityDiscounts.Select(d => new QuantityDiscountResponse(d.MinUsers, d.DiscountPercentage))));

        return Ok(response);
    }

    /// <summary>
    /// Obtém um módulo específico por código
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ModuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModuleByCode(string code, CancellationToken ct)
    {
        var module = await _moduleRepository.GetByCodeAsync(code, ct);

        if (module is null)
            return NotFound(new { message = "Módulo não encontrado" });

        var response = new ModuleResponse(
            module.Id,
            module.Code,
            module.Name,
            module.Description,
            module.PricePerUser,
            module.IsActive,
            module.QuantityDiscounts.Select(d => new QuantityDiscountResponse(d.MinUsers, d.DiscountPercentage)));

        return Ok(response);
    }
}
