using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    public DateTime DatePlaced { get; set; }

    /// <summary>
    /// Tracks session ID for cart-like temporary or multi-session state persistence.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Username or User ID of the creator.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Order status: Pending, Processing, Completed, Cancelled.
    /// </summary>
    public string Status { get; set; } = "Pending";

    // List of items in the order
    public List<InventoryItem> Items { get; set; } = new();

    /// <summary>
    /// Adds an inventory item to the order and synchronizes the relationship references.
    /// </summary>
    public void AddItem(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!Items.Contains(item))
        {
            Items.Add(item);
            item.OrderId = this.OrderId;
            item.Order = this;
        }
    }

    /// <summary>
    /// Removes an inventory item from the order by item ID.
    /// </summary>
    public bool RemoveItem(int itemId)
    {
        var item = Items.FirstOrDefault(i => i.ItemId == itemId);
        if (item != null)
        {
            item.OrderId = null;
            item.Order = null;
            return Items.Remove(item);
        }

        return false;
    }

    /// <summary>
    /// Returns a formatted summary of the order.
    /// Example output: Order #1001 for Samir | Items: 2 | Placed: 4/5/2025
    /// </summary>
    public string GetOrderSummary()
    {
        return $"Order #{OrderId} for {CustomerName} | Items: {Items.Count} | Placed: {DatePlaced.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
