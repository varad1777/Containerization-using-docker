using Microsoft.AspNetCore.Identity;
using MyApp.Infrastructure;
using MyApp.Infrastructure.RTC;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Call the extension method
builder.Services.AddInfrastructureServices(builder.Configuration);

// Controllers, Swagger, CORS, etc.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Run pending migrations + seed roles BEFORE starting to accept requests
await app.MigrateAndSeedOnStartupAsync(new[] { "Admin", "User", "SuperAdmin" });

// Middleware
app.UseHttpsRedirection();  
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<NotificationHub>("/notificationHub");
});

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "User", "SuperAdmin" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}

app.Run();



// ------------------------
// Extension: migrations + seeding with retries
// ------------------------


public static class HostMigrationExtensions
{
    public static async Task MigrateAndSeedOnStartupAsync(this WebApplication app, string[] rolesToEnsure, int maxAttempts = 12, int initialDelaySeconds = 5)
    {
        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("StartupMigrations");
        int attempt = 0;
        var delay = TimeSpan.FromSeconds(initialDelaySeconds);

        // Common fallback: known DbContext type full name (from your logs)
        const string fallbackDbContextTypeName = "MyApp.Infrastructure.Data.AppDbContext, MyApp.Infrastructure";

        while (true)
        {
            attempt++;
            try
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;

                // Try the easy way first
                var dbContexts = services.GetServices<DbContext>().ToArray();
                logger?.LogInformation("GetServices<DbContext> returned {Count}.", dbContexts.Length);

                // If none found, try to resolve a concrete DbContext by type name (fallback)
                if (!dbContexts.Any())
                {
                    logger?.LogWarning("No DbContext instances found via GetServices<DbContext>(). Trying fallback by type name...");
                    var ctxType = Type.GetType(fallbackDbContextTypeName, throwOnError: false, ignoreCase: true);
                    if (ctxType != null && typeof(DbContext).IsAssignableFrom(ctxType))
                    {
                        var instance = services.GetService(ctxType) as DbContext;
                        if (instance != null)
                        {
                            dbContexts = new[] { instance };
                            logger?.LogInformation("Resolved fallback DbContext type {TypeName}.", ctxType.FullName);
                        }
                        else
                        {
                            logger?.LogWarning("Fallback type {TypeName} was found but not registered in DI.", ctxType.FullName);
                        }
                    }
                    else
                    {
                        logger?.LogWarning("Fallback DbContext type '{Name}' not found or not assignable to DbContext.", fallbackDbContextTypeName);
                    }
                }

                if (!dbContexts.Any())
                {
                    logger?.LogWarning("No DbContext services were resolved this attempt. Will retry.");
                    throw new InvalidOperationException("No DbContext resolved.");
                }

                // Migrate each context (fresh instances)
                foreach (var context in dbContexts)
                {
                    var typeName = context.GetType().FullName ?? context.GetType().Name;
                    logger?.LogInformation("Attempting to migrate DbContext {Context} (attempt {Attempt})...", typeName, attempt);
                    await context.Database.MigrateAsync();
                    logger?.LogInformation("Migrated DbContext {Context}.", typeName);
                }

                // Seed roles
                var roleManager = services.GetService<RoleManager<IdentityRole>>();
                if (roleManager is not null && rolesToEnsure?.Length > 0)
                {
                    logger?.LogInformation("Seeding roles...");
                    foreach (var roleName in rolesToEnsure)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            var res = await roleManager.CreateAsync(new IdentityRole(roleName));
                            if (res.Succeeded)
                                logger?.LogInformation("Created role '{RoleName}'.", roleName);
                            else
                                logger?.LogWarning("Failed to create role '{RoleName}': {Errors}", roleName, string.Join(", ", res.Errors.Select(e => e.Description)));
                        }
                    }
                }
                else
                {
                    logger?.LogDebug("RoleManager not available or no roles specified.");
                }

                logger?.LogInformation("Migrations + seeding completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Migration attempt {Attempt} failed. Will retry after {Delay}.", attempt, delay);

                if (attempt >= maxAttempts)
                {
                    logger?.LogError(ex, "Exceeded maximum migration attempts ({MaxAttempts}). Throwing and stopping startup.", maxAttempts);
                    throw;
                }

                await Task.Delay(delay);
                delay = delay + TimeSpan.FromSeconds(5);
            }
        }
    }
}

