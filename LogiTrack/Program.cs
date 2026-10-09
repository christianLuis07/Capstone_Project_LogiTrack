using LogiTrack;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext with SQLite
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));

// Add services to the container
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ==========================================
// CAPSTONE ACTIVITY TESTS (Steps 2, 3, & 5)
// ==========================================

Console.WriteLine("==================================================");
Console.WriteLine("Step 2 Test: InventoryItem DisplayInfo()");
Console.WriteLine("==================================================");
var testItem = new InventoryItem
{
    ItemId = 1,
    Name = "Pallet Jack",
    Quantity = 12,
    Location = "Warehouse A"
};
testItem.DisplayInfo();

Console.WriteLine();
Console.WriteLine("==================================================");
Console.WriteLine("Step 3 Test: Order Add/Remove & Summary");
Console.WriteLine("==================================================");
var testOrder = new Order
{
    OrderId = 1001,
    CustomerName = "Samir",
    DatePlaced = new DateTime(2025, 4, 5)
};
var item1 = new InventoryItem { ItemId = 101, Name = "Pallet Jack", Quantity = 2, Location = "Warehouse A" };
var item2 = new InventoryItem { ItemId = 102, Name = "Forklift", Quantity = 1, Location = "Warehouse B" };
var item3 = new InventoryItem { ItemId = 103, Name = "Hand Truck", Quantity = 4, Location = "Warehouse C" };

testOrder.AddItem(item1);
testOrder.AddItem(item2);
testOrder.AddItem(item3);
testOrder.RemoveItem(103); // Remove Hand Truck, leaving 2 items

Console.WriteLine(testOrder.GetOrderSummary());

Console.WriteLine();
Console.WriteLine("==================================================");
Console.WriteLine("Step 5 Test: Seed and Verify Database");
Console.WriteLine("==================================================");
using (var context = new LogiTrackContext())
{
    // Add test inventory item if none exist
    if (!context.InventoryItems.Any())
    {
        context.InventoryItems.Add(new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        });

        context.SaveChanges();
    }

    // Retrieve and print inventory to confirm
    var items = context.InventoryItems.ToList();
    foreach (var item in items)
    {
        item.DisplayInfo(); // Should print: Item: Pallet Jack | Quantity: 12 | Location: Warehouse A
    }
}
Console.WriteLine("==================================================");
Console.WriteLine();

// ==========================================
// API Endpoints for upcoming Part 2
// ==========================================

app.MapGet("/", () => Results.Ok(new
{
    Application = "LogiTrack Order Management System",
    Status = "Healthy",
    Version = "Part 1 Complete"
}));

app.MapGet("/api/inventory", async (LogiTrackContext db) =>
    await db.InventoryItems.ToListAsync());

app.MapGet("/api/orders", async (LogiTrackContext db) =>
    await db.Orders.Include(o => o.Items).ToListAsync());

app.Run();
