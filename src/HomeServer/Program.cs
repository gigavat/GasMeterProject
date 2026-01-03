using Microsoft.EntityFrameworkCore;
using MetricsApi.Data;
using MetricsApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register audit service
builder.Services.AddScoped<MetricsApi.Services.IAuditService, MetricsApi.Services.AuditService>();

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add API Key middleware (must come before audit middleware to validate early)
app.UseMiddleware<ApiKeyMiddleware>();

// Add audit middleware to track all API requests
app.UseMiddleware<AuditMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created and log connection event
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MetricsDbContext>();
        context.Database.EnsureCreated();
        
        // Log application startup/connection event
        var auditService = services.GetRequiredService<MetricsApi.Services.IAuditService>();
        await auditService.LogEventAsync(
            eventType: "Connected",
            description: "Application started and database connection established",
            source: "System",
            additionalData: $"Environment: {app.Environment.EnvironmentName}"
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
        
        // Try to log the error to audit if possible
        try
        {
            var auditService = services.GetRequiredService<MetricsApi.Services.IAuditService>();
            await auditService.LogEventAsync(
                eventType: "Error",
                description: $"Database initialization error: {ex.Message}",
                source: "System",
                statusCode: 500,
                additionalData: ex.ToString()
            );
        }
        catch
        {
            // If audit logging fails, just continue
        }
    }
}

app.Run();

