using eShopModernized.Models;
using eShopModernized.Services;
using Microsoft.AspNetCore.Mvc;

namespace eShopModernized.Controllers;

/// <summary>
/// Inventory API Controller — exposes business logic that was previously 
/// locked inside stored procedures as a REST API.
/// 
/// This is a NEW capability that didn't exist in the legacy app.
/// The legacy app's sp_UpdateInventory and sp_GetInventoryReport were only
/// accessible via direct SQL calls. Now they're available as HTTP endpoints,
/// enabling microservice decomposition in the future.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Update stock for a catalog item.
    /// Replaces direct sp_UpdateInventory stored procedure calls.
    /// 
    /// POST /api/inventory/{id}/adjust
    /// Body: { "quantityChange": -5 }  (negative = sale, positive = restock)
    /// </summary>
    [HttpPost("{id}/adjust")]
    [ProducesResponseType(typeof(InventoryUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryUpdateResult>> AdjustStock(
        int id,
        [FromBody] StockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _inventoryService.UpdateStockAsync(
                id, request.QuantityChange, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("does not exist"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get inventory report with brand summaries and reorder identification.
    /// Replaces direct sp_GetInventoryReport stored procedure calls.
    /// 
    /// GET /api/inventory/report
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(InventoryReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryReport>> GetReport(CancellationToken cancellationToken = default)
    {
        var report = await _inventoryService.GetInventoryReportAsync(cancellationToken);
        return Ok(report);
    }
}

public record StockAdjustmentRequest(int QuantityChange);
