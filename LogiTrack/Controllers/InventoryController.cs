using System.Diagnostics;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;
    private const string InventoryCacheKey = "Inventory_List_CacheKey";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public InventoryController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// GET: /api/inventory
    /// Returns a list of all inventory items.
    /// Utilizes in-memory caching for 30 seconds and EF Core AsNoTracking() for optimal query performance.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllItems()
    {
        var stopwatch = Stopwatch.StartNew();

        if (_cache.TryGetValue(InventoryCacheKey, out List<InventoryItem>? cachedItems) && cachedItems != null)
        {
            stopwatch.Stop();
            Response.Headers.Append("X-Cache", "HIT");
            Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
            return Ok(cachedItems);
        }

        // Cache miss: Query database with AsNoTracking() optimization
        var items = await _context.InventoryItems
            .AsNoTracking()
            .ToListAsync();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(InventoryCacheKey, items, cacheEntryOptions);

        stopwatch.Stop();
        Response.Headers.Append("X-Cache", "MISS");
        Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());

        return Ok(items);
    }

    /// <summary>
    /// GET: /api/inventory/{id}
    /// Returns an inventory item by ID with AsNoTracking() optimization.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryItem>> GetItemById(int id)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ItemId == id);

        if (item == null)
        {
            return NotFound(new { message = $"Inventory item with ID {id} was not found." });
        }

        return Ok(item);
    }

    /// <summary>
    /// POST: /api/inventory
    /// Adds a new item to the inventory. Restricted to users with the "Manager" role.
    /// Automatically invalidates the inventory cache to ensure data freshness.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItem>> CreateItem([FromBody] InventoryItem item)
    {
        if (item == null)
        {
            return BadRequest(new { message = "Item payload cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        // Invalidate cache on modification
        _cache.Remove(InventoryCacheKey);

        return CreatedAtAction(nameof(GetItemById), new { id = item.ItemId }, item);
    }

    /// <summary>
    /// DELETE: /api/inventory/{id}
    /// Removes an item by ID. Restricted to users with the "Manager" role.
    /// Invalidate the inventory cache on deletion.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Inventory item with ID {id} was not found." });
        }

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();

        // Invalidate cache on modification
        _cache.Remove(InventoryCacheKey);

        return Ok(new { message = $"Inventory item with ID {id} successfully deleted." });
    }
}
