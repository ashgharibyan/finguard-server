using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using finguard_server.Data;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Log all environment variables
Console.WriteLine("Environment Variables:");
foreach (var keyvar in Environment.GetEnvironmentVariables().Keys)
{
    Console.WriteLine($"{keyvar}: {Environment.GetEnvironmentVariable(keyvar.ToString())}");
}

// Configure port for Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Build the PostgreSQL connection string using Railway environment variables
var connectionString = BuildConnectionString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Controllers and Swagger
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

// JWT Configuration - Simplified direct approach
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
Console.WriteLine($"JWT Key length: {jwtKey?.Length ?? 0}");  // Log key length for debugging

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT_KEY environment variable is not set");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,  // Changed to false since we don't have issuer config
        ValidateAudience = false, // Changed to false since we don't have audience config
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
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

// Add global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exceptionHandlerPathFeature.Error, "An unhandled exception occurred");
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An internal server error occurred.",
                details = app.Environment.IsDevelopment() ? exceptionHandlerPathFeature.Error.Message : null
            });
        }
    });
});

// Database initialization
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

// Middleware configuration
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Incoming {context.Request.Method} request to {context.Request.Path}");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string BuildConnectionString()
{
    // First try to use the complete DATABASE_URL if available
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        return databaseUrl;
    }

    // Fallback to building from individual components
    var host = Environment.GetEnvironmentVariable("PGHOST") ?? throw new InvalidOperationException("PGHOST is not set");
    var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var username = Environment.GetEnvironmentVariable("PGUSER") ?? throw new InvalidOperationException("PGUSER is not set");
    var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? throw new InvalidOperationException("PGPASSWORD is not set");
    var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? throw new InvalidOperationException("PGDATABASE is not set");

    return $"Server={host};Port={port};User Id={username};Password={password};Database={database};SslMode=Require;TrustServerCertificate=True";
}