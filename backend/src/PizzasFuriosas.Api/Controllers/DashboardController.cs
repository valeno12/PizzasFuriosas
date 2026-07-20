using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize(Policy = AppPolicies.AdminOnly)]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<BalanceResponse>>> GetBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var balance = await dashboardService.GetBalanceAsync(from, to, cancellationToken);
        return Ok(new ApiResponse<BalanceResponse>(balance));
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<StatisticsResponse>>> GetStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var stats = await dashboardService.GetStatisticsAsync(from, to, cancellationToken);
        return Ok(new ApiResponse<StatisticsResponse>(stats));
    }
}
