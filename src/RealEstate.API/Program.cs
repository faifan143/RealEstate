using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RealEstate.API;
using RealEstate.API.Extensions;
using RealEstate.API.Middleware;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;
using RealEstate.Infrastructure.Data;
using RealEstate.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure PostgreSQL connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with phone number as username
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    
    // Configure for phone number login
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")))
    };
});

// Register custom services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add HttpContextAccessor for accessing the request context in services
builder.Services.AddHttpContextAccessor();

// Configure AutoMapper with HttpContextAccessor
builder.Services.ConfigureAutoMapper();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RealEstate API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply database migrations before starting the app
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("Running database migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during startup: {ex.Message}");
        // Continue with application startup even if migrations fail
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RealEstate API v1"));
}

app.UseHttpsRedirection();

// Configure static file serving for uploaded images
app.UseStaticFiles();

// Execute SQL to add missing columns
DatabaseUpdater.AddMissingColumns(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string not found"));

// Use custom exception handling middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseRouting();

// Enable CORS
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed database with admin user only (no roles needed for queue system)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create Admin role for image approval only
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Create admin user
        var adminPhone = "123456789";
        var adminUser = await userManager.FindByNameAsync(adminPhone);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminPhone,
                PhoneNumber = adminPhone,
                FullName = "المدير",
                Email = "admin@realestate.com",
                PhoneNumberConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}



// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}


// Create directories for image uploads if they don't exist
var imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
var propertiesImagesDirectory = Path.Combine(imagesDirectory, "properties");

if (!Directory.Exists(imagesDirectory))
{
    Directory.CreateDirectory(imagesDirectory);
}

if (!Directory.Exists(propertiesImagesDirectory))
{
    Directory.CreateDirectory(propertiesImagesDirectory);
}

// Start the application and display server information
await app.StartAsync();

// Get server addresses
var serverAddressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
var addresses = serverAddressesFeature?.Addresses;

Console.WriteLine("\n========== SERVER INFORMATION ==========");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Application Name: RealEstate API");
Console.WriteLine($"Start Time: {DateTime.UtcNow}");

if (addresses != null && addresses.Any())
{
    Console.WriteLine("\nServer Endpoints:");
    foreach (var address in addresses)
    {
        Console.WriteLine($"  - {address}");
    }
}

// Display local IP addresses
Console.WriteLine("\nLocal Network Interfaces:");
var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
    .Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
    .ToList();

foreach (var nic in networkInterfaces)
{
    var ipProps = nic.GetIPProperties();
    var ipAddresses = ipProps.UnicastAddresses
        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(addr => addr.Address.ToString())
        .ToList();

    if (ipAddresses.Any())
    {
        Console.WriteLine($"  - {nic.Name}: {string.Join(", ", ipAddresses)}");
    }
}

Console.WriteLine("\nSwagger UI: http://localhost:5268/swagger");
Console.WriteLine("\nPress Ctrl+C to shut down.");
Console.WriteLine("=======================================\n");

await app.WaitForShutdownAsync();
