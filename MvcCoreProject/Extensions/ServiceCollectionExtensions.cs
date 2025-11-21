using CoreProject.Context;
using CoreProject.Models;
using CoreProject.Repositories;
using CoreProject.Repositories.Interfaces;
using CoreProject.Services;
using CoreProject.Services.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace MvcCoreProject.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Database Context (Entity Framework Core with SQL Server)
        /// </summary>
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("RemoteConnection")));

            return services;
        }

        /// <summary>
        /// Add ASP.NET Core Identity with custom ApplicationUser and int keys
        /// </summary>
        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            return services;
        }

        /// <summary>
        /// Add JWT Authentication with cookie fallback for web app
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            services.AddAuthentication(options =>
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
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // First, try to get token from Authorization header (for API calls)
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        {
                            context.Token = authHeader.Substring("Bearer ".Length).Trim();
                        }
                        // Fallback to cookie (for web app)
                        else if (context.Request.Cookies.TryGetValue("VisionValley_JWT", out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Check if this is an API request
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            return Task.CompletedTask;
                        }

                        // Web request - redirect to home
                        context.HandleResponse();
                        context.Response.Redirect("/Home/Index");
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Add all Repository registrations
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Generic Repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Specific Repositories
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<ITimetableRepository, TimetableRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<ILampRepository, LampRepository>();

            return services;
        }

        /// <summary>
        /// Add all Business Service registrations
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Authentication Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuthApiService, AuthApiService>();

            // Business Services
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IAttendanceApiService, AttendanceApiService>();
            services.AddScoped<ITimetableService, TimetableService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<ITimezoneService, TimezoneService>();
            services.AddScoped<ILampService, LampService>();

            return services;
        }

        /// <summary>
        /// Add Lamp WebSocket and Background Scheduler services
        /// </summary>
        public static IServiceCollection AddLampServices(this IServiceCollection services)
        {
            services.AddSingleton<LampWebSocketHandler>();
            services.AddHostedService<LampSchedulerService>();

            return services;
        }

        /// <summary>
        /// Add Swagger/OpenAPI with JWT Bearer authentication
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.\r\n\r\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            return services;
        }

        /// <summary>
        /// Add CORS policy for API access from hardware devices
        /// </summary>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}
