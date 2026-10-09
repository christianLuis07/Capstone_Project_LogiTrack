using System.Text;
using System.Text.Json.Serialization;
using LogiTrack;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Register EF Core DbContext with SQLite
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));

// 2. Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<LogiTrackContext>()
.AddDefaultTokenProviders();

// 3. Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "LogiTrackSuperSecretSecurityKey2026!MustBeAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LogiTrackAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LogiTrackClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Enable In-Memory Caching (Part 4 Performance Optimization)
builder.Services.AddMemoryCache();

// 5. Register Controllers & JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 5. Configure Swagger / OpenAPI with JWT Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LogiTrack Order Management API",
        Version = "v1",
        Description = "LogiTrack Order & Inventory Management Secured API with JWT & Roles (Part 3)"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
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

// Authentication MUST be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Health check and root route
app.MapGet("/", () => Results.Ok(new
{
    Application = "LogiTrack Order Management System",
    Status = "Healthy",
    Version = "Part 3 Active (Secured with ASP.NET Identity & JWT)",
    SwaggerUI = "/swagger",
    AuthEndpoints = new[]
    {
        "POST /api/auth/register",
        "POST /api/auth/login"
    },
    ProtectedEndpoints = new[]
    {
        "GET /api/inventory [Authorize]",
        "POST /api/inventory [Authorize(Roles = 'Manager')]",
        "DELETE /api/inventory/{id} [Authorize(Roles = 'Manager')]",
        "GET /api/orders [Authorize]",
        "POST /api/orders [Authorize]",
        "DELETE /api/orders/{id} [Authorize(Roles = 'Manager')]"
    }
}));

// ==========================================
// SEEDING AND VERIFICATION TESTS
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LogiTrackContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // 1. Seed Roles
    string[] roleNames = { "Manager", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // 2. Seed Default Manager User
    if (await userManager.FindByNameAsync("manager") == null)
    {
        var managerUser = new ApplicationUser
        {
            UserName = "manager",
            Email = "manager@logitrack.com",
            FullName = "Warehouse Manager"
        };
        var createResult = await userManager.CreateAsync(managerUser, "Manager123!");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(managerUser, "Manager");
        }
    }

    // 3. Seed Default Regular User
    if (await userManager.FindByNameAsync("user") == null)
    {
        var regularUser = new ApplicationUser
        {
            UserName = "user",
            Email = "user@logitrack.com",
            FullName = "Logistics Staff"
        };
        var createResult = await userManager.CreateAsync(regularUser, "User123!");
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(regularUser, "User");
        }
    }

    // 4. Initial Inventory Seeding & Tests
    if (!context.InventoryItems.Any())
    {
        context.InventoryItems.Add(new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        });
        await context.SaveChangesAsync();
    }

    Console.WriteLine("==================================================");
    Console.WriteLine("LogiTrack System Ready - Part 3 Security Configured");
    Console.WriteLine("Seed users: manager (Role: Manager) | user (Role: User)");
    Console.WriteLine("==================================================");
}

app.Run();
