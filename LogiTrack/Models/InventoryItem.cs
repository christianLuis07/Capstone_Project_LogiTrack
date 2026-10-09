using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models;

public class InventoryItem
{
    [Key]
    public int ItemId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    // Optional foreign key to Order for EF Core one-to-many relationship
    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    /// <summary>
    /// Displays item information in the standard LogiTrack format.
    /// Format: Item: Pallet Jack | Quantity: 12 | Location: Warehouse A
    /// </summary>
    public void DisplayInfo()
    {
        Console.WriteLine($"Item: {Name} | Quantity: {Quantity} | Location: {Location}");
    }
}
