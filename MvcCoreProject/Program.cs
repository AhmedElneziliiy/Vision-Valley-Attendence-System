using FaceRecognition.Core;
using FaceRecognition.Core.Configuration;
using MvcCoreProject.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERVICE REGISTRATIONS
// ============================================

// Controllers (MVC + API)
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
// Database
builder.Services.AddDatabaseContext(builder.Configuration);

// Identity & Authentication
builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Repositories
builder.Services.AddRepositories();

// Business Services
builder.Services.AddBusinessServices();

// Lamp Services (WebSocket + Background Scheduler)
builder.Services.AddLampServices();

// Swagger/OpenAPI
builder.Services.AddSwaggerConfiguration();

// CORS
builder.Services.AddCorsConfiguration();

builder.Services.Configure<FaceRecognitionOptions>(
    builder.Configuration.GetSection("FaceRecognition"));
builder.Services.AddFaceRecognition();
// ============================================
// APPLICATION PIPELINE
// ============================================

var app = builder.Build();

// Exception Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Static Files
app.UseStaticFiles();

// WebSocket Support (for ESP32 lamps)
app.UseWebSocketConfiguration();

// Routing
app.UseRouting();

// Swagger UI
app.UseSwaggerConfiguration();

// CORS
app.UseCors("AllowAll");

// JWT Cookie Middleware
app.UseJwtCookieMiddleware();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Lamp WebSocket Middleware
app.UseLampWebSocket();

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
