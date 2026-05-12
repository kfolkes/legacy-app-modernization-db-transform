using eShopModernized.Models;
using eShopModernized.Repositories;
using eShopModernized.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace eShopModernized.Controllers;

/// <summary>
/// Modernized Catalog Controller — replaces legacy CatalogController.
/// 
/// MIGRATION CHANGES:
///   - System.Web.Mvc.Controller          → Microsoft.AspNetCore.Mvc.Controller
///   - HttpStatusCodeResult(BadRequest)   → BadRequest()
///   - HttpNotFound()                     → NotFound()
///   - Synchronous actions                → Async (Task<IActionResult>)
///   - ViewBag for dropdowns              → Strongly-typed ViewData with SelectList
///   - [Bind(Include="...")] overposting  → Input DTO pattern (CatalogItemInput)
///   - static ILog (log4net)              → ILogger<T> (DI injected)
///   - ICatalogService                    → ICatalogRepository + IInventoryService
///   - string interpolation logging       → Structured message templates
/// </summary>
public class CatalogController : Controller
{
    private readonly ICatalogRepository _repository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<CatalogController> _logger;
    private readonly CatalogSettings _settings;

    public CatalogController(
        ICatalogRepository repository,
        IInventoryService inventoryService,
        ILogger<CatalogController> logger,
        IOptions<CatalogSettings> settings)
    {
        _repository = repository;
        _inventoryService = inventoryService;
        _logger = logger;
        _settings = settings.Value;
    }

    // GET /Catalog[?pageSize=10&pageIndex=0]
    public async Task<IActionResult> Index(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading catalog index. PageSize={PageSize}, PageIndex={PageIndex}", pageSize, pageIndex);
        
        var paginatedItems = await _repository.GetItemsAsync(pageSize, pageIndex, cancellationToken);
        SetPictureUris(paginatedItems.Data);
        
        return View(paginatedItems);
    }

    // GET: Catalog/Details/5
    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading catalog item details. Id={Id}", id);

        if (id == null)
            return BadRequest();

        var item = await _repository.GetByIdAsync(id.Value, cancellationToken);
        if (item == null)
            return NotFound();

        SetPictureUri(item);
        return View(item);
    }

    // GET: Catalog/Create
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading catalog item create form");

        await PopulateDropdowns(cancellationToken: cancellationToken);
        return View(new CatalogItem());
    }

    // POST: Catalog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CatalogItemInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating catalog item. Name={Name}", input.Name);

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(input.CatalogBrandId, input.CatalogTypeId, cancellationToken);
            return View(input.ToCatalogItem());
        }

        var item = input.ToCatalogItem();
        await _repository.AddAsync(item, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    // GET: Catalog/Edit/5
    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading catalog item edit form. Id={Id}", id);

        if (id == null)
            return BadRequest();

        var item = await _repository.GetByIdAsync(id.Value, cancellationToken);
        if (item == null)
            return NotFound();

        SetPictureUri(item);
        await PopulateDropdowns(item.CatalogBrandId, item.CatalogTypeId, cancellationToken);
        return View(item);
    }

    // POST: Catalog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, CatalogItemInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating catalog item. Id={Id}", id);

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(input.CatalogBrandId, input.CatalogTypeId, cancellationToken);
            return View(input.ToCatalogItem());
        }

        var item = input.ToCatalogItem();
        item.Id = id;
        await _repository.UpdateAsync(item, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    // GET: Catalog/Delete/5
    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading catalog item delete confirmation. Id={Id}", id);

        if (id == null)
            return BadRequest();

        var item = await _repository.GetByIdAsync(id.Value, cancellationToken);
        if (item == null)
            return NotFound();

        SetPictureUri(item);
        return View(item);
    }

    // POST: Catalog/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting catalog item. Id={Id}", id);
        await _repository.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    // ---- Private Helpers ----

    private async Task PopulateDropdowns(
        int? selectedBrandId = null, int? selectedTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var brands = await _repository.GetBrandsAsync(cancellationToken);
        var types = await _repository.GetTypesAsync(cancellationToken);
        
        ViewData["CatalogBrandId"] = new SelectList(brands, "Id", "Brand", selectedBrandId);
        ViewData["CatalogTypeId"] = new SelectList(types, "Id", "Type", selectedTypeId);
    }

    private void SetPictureUris(IEnumerable<CatalogItem> items)
    {
        foreach (var item in items)
            SetPictureUri(item);
    }

    private void SetPictureUri(CatalogItem item)
    {
        item.PictureFileName = $"{_settings.PicBaseUrl}{item.PictureFileName}";
    }
}

/// <summary>
/// Input DTO for catalog item creation/editing.
/// Replaces [Bind(Include="...")] over-posting prevention pattern.
/// Only includes fields the user should be able to set.
/// </summary>
public class CatalogItemInput
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 1000000)]
    public decimal Price { get; set; }

    public string PictureFileName { get; set; } = CatalogItem.DefaultPictureName;
    public int CatalogTypeId { get; set; }
    public int CatalogBrandId { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0, 10000000)]
    public int AvailableStock { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0, 10000000)]
    public int RestockThreshold { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0, 10000000)]
    public int MaxStockThreshold { get; set; }
    
    public bool OnReorder { get; set; }

    public CatalogItem ToCatalogItem() => new()
    {
        Name = Name,
        Description = Description,
        Price = Price,
        PictureFileName = PictureFileName,
        CatalogTypeId = CatalogTypeId,
        CatalogBrandId = CatalogBrandId,
        AvailableStock = AvailableStock,
        RestockThreshold = RestockThreshold,
        MaxStockThreshold = MaxStockThreshold,
        OnReorder = OnReorder
    };
}
