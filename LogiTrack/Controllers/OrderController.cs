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
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;
    private const string OrdersCacheKey = "Orders_List_CacheKey";
    private const string InventoryCacheKey = "Inventory_List_CacheKey";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public OrderController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// GET: /api/orders
    /// Returns a list of all orders including their associated items.
    /// Optimized with in-memory caching and EF Core .AsNoTracking() to eliminate entity tracking overhead.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var stopwatch = Stopwatch.StartNew();

        if (_cache.TryGetValue(OrdersCacheKey, out List<Order>? cachedOrders) && cachedOrders != null)
        {
            stopwatch.Stop();
            Response.Headers.Append("X-Cache", "HIT");
            Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
            return Ok(cachedOrders);
        }

        // Cache miss: Execute optimized eager-loading query with AsNoTracking()
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ToListAsync();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(OrdersCacheKey, orders, cacheEntryOptions);

        stopwatch.Stop();
        Response.Headers.Append("X-Cache", "MISS");
        Response.Headers.Append("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());

        return Ok(orders);
    }

    /// <summary>
    /// GET: /api/orders/{id}
    /// Returns a specific order by ID with its items using .AsNoTracking() for optimal read speed.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} was not found." });
        }

        return Ok(order);
    }

    /// <summary>
    /// POST: /api/orders
    /// Creates a new order. Optimized to batch-query existing items into a dictionary,
    /// completely eliminating the N+1 database round-trip problem.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        if (order == null)
        {
            return BadRequest(new { message = "Order payload cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (order.DatePlaced == default)
        {
            order.DatePlaced = DateTime.UtcNow;
        }

        // Query Optimization: Batch-fetch existing item IDs to eliminate N+1 queries
        if (order.Items != null && order.Items.Any())
        {
            var existingIds = order.Items
                .Where(i => i.ItemId > 0)
                .Select(i => i.ItemId)
                .Distinct()
                .ToList();

            Dictionary<int, InventoryItem> existingItemsDict = new();
            if (existingIds.Any())
            {
                existingItemsDict = await _context.InventoryItems
                    .Where(i => existingIds.Contains(i.ItemId))
                    .ToDictionaryAsync(i => i.ItemId);
            }

            var itemsToAttach = new List<InventoryItem>();
            foreach (var item in order.Items)
            {
                if (item.ItemId > 0 && existingItemsDict.TryGetValue(item.ItemId, out var existingItem))
                {
                    itemsToAttach.Add(existingItem);
                }
                else
                {
                    itemsToAttach.Add(item);
                }
            }
            order.Items = itemsToAttach;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Invalidate orders and inventory cache upon new order creation
        _cache.Remove(OrdersCacheKey);
        _cache.Remove(InventoryCacheKey);

        return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
    }

    /// <summary>
    /// DELETE: /api/orders/{id}
    /// Deletes an order by ID. Restricted to users with the "Manager" role.
    /// Invalidates caches upon successful deletion.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} was not found." });
        }

        // Release associated items back to general inventory
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                item.OrderId = null;
            }
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        // Invalidate caches
        _cache.Remove(OrdersCacheKey);
        _cache.Remove(InventoryCacheKey);

        return Ok(new { message = $"Order with ID {id} successfully deleted." });
    }
}
