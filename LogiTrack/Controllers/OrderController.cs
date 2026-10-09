using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly LogiTrackContext _context;

    public OrderController(LogiTrackContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET: /api/orders
    /// Returns a list of all orders including their associated items.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// GET: /api/orders/{id}
    /// Returns a specific order by ID including its items.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {
        var order = await _context.Orders
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
    /// Creates a new order. Can include multiple inventory items.
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

        // Process inventory items submitted with the order
        if (order.Items != null && order.Items.Any())
        {
            var itemsToAttach = new List<InventoryItem>();
            foreach (var item in order.Items)
            {
                if (item.ItemId > 0)
                {
                    var existingItem = await _context.InventoryItems.FindAsync(item.ItemId);
                    if (existingItem != null)
                    {
                        itemsToAttach.Add(existingItem);
                        continue;
                    }
                }
                itemsToAttach.Add(item);
            }
            order.Items = itemsToAttach;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
    }

    /// <summary>
    /// DELETE: /api/orders/{id}
    /// Deletes an order by ID.
    /// </summary>
    [HttpDelete("{id}")]
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

        return Ok(new { message = $"Order with ID {id} successfully deleted." });
    }
}
