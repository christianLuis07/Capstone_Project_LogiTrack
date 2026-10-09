using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;

    public InventoryController(LogiTrackContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET: /api/inventory
    /// Returns a list of all inventory items.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllItems()
    {
        var items = await _context.InventoryItems.ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// GET: /api/inventory/{id}
    /// Returns an inventory item by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryItem>> GetItemById(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Inventory item with ID {id} was not found." });
        }

        return Ok(item);
    }

    /// <summary>
    /// POST: /api/inventory
    /// Adds a new item to the inventory.
    /// </summary>
    [HttpPost]
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

        return CreatedAtAction(nameof(GetItemById), new { id = item.ItemId }, item);
    }

    /// <summary>
    /// DELETE: /api/inventory/{id}
    /// Removes an item by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Inventory item with ID {id} was not found." });
        }

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Inventory item with ID {id} successfully deleted." });
    }
}
