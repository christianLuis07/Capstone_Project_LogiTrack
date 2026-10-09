using System.Text.Json.Serialization;
using LogiTrack;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext with SQLite
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));

// Register Controllers and handle cyclic references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "LogiTrack Order Management API",
        Version = "v1",
        Description = "LogiTrack Order and Inventory Management Web API (Capstone Part 2)"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogiTrack API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Health check and route discovery landing endpoint
app.MapGet("/", () => Results.Ok(new
{
    Application = "LogiTrack Order Management System",
    Status = "Healthy",
    Version = "Part 2 Active",
    SwaggerUI = "/swagger",
    Endpoints = new[]
    {
        "GET /api/inventory",
        "GET /api/inventory/{id}",
        "POST /api/inventory",
        "DELETE /api/inventory/{id}",
        "GET /api/orders",
        "GET /api/orders/{id}",
        "POST /api/orders",
        "DELETE /api/orders/{id}"
    }
}));

// ==========================================
// CAPSTONE ACTIVITY STARTUP TESTS & SEEDING
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogiTrackContext>();

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
    testOrder.RemoveItem(103);

    Console.WriteLine(testOrder.GetOrderSummary());

    Console.WriteLine();
    Console.WriteLine("==================================================");
    Console.WriteLine("Step 5 Test: Seed and Verify Database");
    Console.WriteLine("==================================================");
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

    var items = context.InventoryItems.ToList();
    foreach (var item in items)
    {
        item.DisplayInfo();
    }
    Console.WriteLine("==================================================");
    Console.WriteLine();
}

app.Run();
