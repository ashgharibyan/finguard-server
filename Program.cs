using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using finguard_server.Data;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Local
// var port = Environment.GetEnvironmentVariable("PORT") ?? "5064"; // Change to 5064 for consistency
// builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Local PostgreSQL connection string
// var connectionString = "Host=localhost;Database=finguard;Username=ashgharibyan;Port=5432";

// Configure port for Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add this before the connection string setup
Console.WriteLine("Environment Variables:");
Console.WriteLine($"PGHOST: {Environment.GetEnvironmentVariable("PGHOST")}");
Console.WriteLine($"PGDATABASE: {Environment.GetEnvironmentVariable("PGDATABASE")}");
Console.WriteLine($"PGUSER: {Environment.GetEnvironmentVariable("PGUSER")}");
Console.WriteLine($"PGPORT: {Environment.GetEnvironmentVariable("PGPORT")}");
Console.WriteLine($"DATABASE_URL: {Environment.GetEnvironmentVariable("DATABASE_URL")}");

// PostgreSQL Configuration - use environment variables for Railway
var connectionString = Environment.GetEnvironmentVariable("PGHOST") != null
    ? $"Host={Environment.GetEnvironmentVariable("PGHOST")};" +
      $"Database={Environment.GetEnvironmentVariable("PGDATABASE")};" +
      $"Username={Environment.GetEnvironmentVariable("PGUSER")};" +
      $"Password={Environment.GetEnvironmentVariable("PGPASSWORD")};" +
      $"Port={Environment.GetEnvironmentVariable("PGPORT")};" +
      "Pooling=true;SSL Mode=Require;Trust Server Certificate=true"
    : "Host=localhost;Database=finguard;Username=ashgharibyan;Port=5432";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with JWT Authentication
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"
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
            new string[] { }
        }
    });
});

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(
    Environment.GetEnvironmentVariable("JWT_KEY") ??
    jwtSettings["Key"] ??
    throw new InvalidOperationException("JWT Key is not configured."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://finguard-client.vercel.app",
            "http://localhost:3000"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");
        context.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");

        if (context.Database.CanConnect())
        {
            logger.LogInformation("Successfully connected to the database.");
        }
        else
        {
            logger.LogError("Cannot connect to the database!");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating or accessing the database.");
    throw;
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();