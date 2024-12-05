using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using finguard_server.Data;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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



// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(
    Environment.GetEnvironmentVariable("JWT_KEY") ??
    jwtSettings["Key"] ??
    throw new InvalidOperationException("JWT Key is not configured."));


Console.WriteLine("--------------------------------------");
Console.WriteLine("--------------------------------------");
Console.WriteLine("--------------------------------------");
Console.WriteLine($"JWT Key retrieved:", Environment.GetEnvironmentVariable("JWT_KEY"));
Console.WriteLine($"HOSTretrieved:", Environment.GetEnvironmentVariable("PGHOST"));
Console.WriteLine($"PGUSER retrieved:", Environment.GetEnvironmentVariable("PGUSER"));
Console.WriteLine($"PGDATABASE retrieved:", Environment.GetEnvironmentVariable("PGDATABASE"));

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

static string BuildConnectionString()
{
    // Use Railway environment variables to build the connection string
    var host = Environment.GetEnvironmentVariable("PGHOST") ?? throw new InvalidOperationException("PGHOST is not set");
    var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var username = Environment.GetEnvironmentVariable("PGUSER") ?? throw new InvalidOperationException("PGUSER is not set");
    var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? throw new InvalidOperationException("PGPASSWORD is not set");
    var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? throw new InvalidOperationException("PGDATABASE is not set");

    Console.WriteLine("--------------------------------------");
    Console.WriteLine("Adasdasdsadsad", host, port, username, password, database);

    return $"Server={host};Port={port};User Id={username};Password={password};Database={database};SslMode=Require;TrustServerCertificate=True";
}
