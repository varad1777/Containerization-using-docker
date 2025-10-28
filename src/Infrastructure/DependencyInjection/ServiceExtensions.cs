    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using MyApp.Application.Interfaces;
    using MyApp.Domain.Entities;
    using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Queues;
using MyApp.Infrastructure.RTC;
    using MyApp.Infrastructure.Services;
    using System.Text;

    namespace MyApp.Infrastructure
    {
        public static class ServiceExtensions
        {
            public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
            {
                // DbContext
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                // Identity
                services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddDefaultTokenProviders();

                // JWT Authentication
                var jwtSettings = configuration.GetSection("JWT");
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                })
                .AddJwtBearer(options =>
                {
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("jwtToken"))
                                context.Token = context.Request.Cookies["jwtToken"];
                            return Task.CompletedTask;
                        }
                    };

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = jwtSettings["ValidIssuer"],
                        ValidAudience = jwtSettings["ValidAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
                    };
                });

                services.AddAuthorization();

                // Application Services
                services.AddScoped<IAssetService, AssetService>();
                services.AddScoped<ISignalService, SignalService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddScoped<ITokenService, TokenService>();

                // SignalR
                services.AddSignalR();

            // Register queue and background service
            //services.AddSingleton<CalculationQueue>();
            services.AddSingleton(sp => new CalculationQueue(capacity: 3));
            services.AddHostedService<AverageCalculationBackgroundService>();

                return services;
            }
        }
    }
