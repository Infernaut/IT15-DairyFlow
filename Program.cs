using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Security;
using IT15_DairyFlow.Security.Crypto;
using IT15_DairyFlow.Services;
using IT15_DairyFlow.Services.Security;
using IT15_DairyFlow.Services.Security.Captcha;
using IT15_DairyFlow.Services.SystemLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using QuestPDF.Infrastructure;

// Configure Serilog early (before host is built)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/dairyflow-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Use Serilog as the logging provider
builder.Host.UseSerilog();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// With DbContext pooling EF resolves interceptors from the root provider, so interceptors must be singleton.
builder.Services.AddSingleton<EntityFieldEncryptionInterceptor>();

// DbContext pooling reduces allocations and also prevents EF from building many internal service providers
// when options differ per context. We also suppress ManyServiceProvidersCreatedWarning from throwing.
builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<EntityFieldEncryptionInterceptor>());
    options.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Sign-in policies
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;

    // Password policies (enterprise-grade baseline)
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 6;

    // Lockout policies
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    // User policies
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Cookie / session hardening
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    // Standard header expected by ASP.NET Core helpers and common JS patterns.
    // Our AJAX calls send the token in this header.
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();
builder.Services.AddScoped<IEncryptionBackfillService, EncryptionBackfillService>();

builder.Services.AddOptions<CryptoSettings>()
    .Bind(builder.Configuration.GetSection(CryptoSettings.SectionName))
    ;
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddSingleton<ILookupHashService, LookupHashService>();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// PayMongo & Email services
builder.Services.AddHttpClient();
builder.Services.AddOptions<CaptchaSettings>()
    .Bind(builder.Configuration.GetSection(CaptchaSettings.SectionName));
builder.Services.AddHttpClient<ICaptchaVerificationService, CaptchaVerificationService>();
builder.Services.AddSingleton<PayMongoService>();
builder.Services.AddSingleton<SubscriptionEmailService>();

// Notification service (scoped — uses DbContext)
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Global error handling for AJAX requests
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            context.Request.Headers["Accept"].ToString().Contains("application/json") ||
            context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception in {Path}", context.Request.Path);
            await context.Response.WriteAsJsonAsync(new { success = false, message = "An unexpected error occurred. Please try again." });
            return;
        }
        throw;
    }

    // Handle 404 for non-AJAX requests
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        context.Request.Path = "/Home/Error";
        await next();
    }
});

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

// Operational system logs (separate from audit logs)
// Captures request health/performance for SuperAdmin transparency.
app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        sw.Stop();

        var path = context.Request.Path.Value ?? string.Empty;
        var isStatic = path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

        if (!isStatic)
        {
            using var scope = context.RequestServices.CreateScope();
            var systemLogs = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

            var status = context.Response?.StatusCode ?? 0;
            var level = status >= 500 ? "Error" : status >= 400 ? "Warning" : "Information";
            var msg = $"{context.Request.Method} {path} → {status}";

            await systemLogs.LogAsync(
                component: "Web",
                eventName: "RequestCompleted",
                level: level,
                message: msg,
                durationMs: sw.ElapsedMilliseconds,
                correlationId: context.TraceIdentifier,
                metadata: new Dictionary<string, object?>
                {
                    ["StatusCode"] = status,
                    ["Method"] = context.Request.Method,
                    ["Path"] = path
                });
        }
    }
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();

// Prevent browser back-button from showing stale authenticated pages
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// SignalR Hub endpoints
app.MapHub<DairyFlowHub>("/hubs/dairyflow");

app.Run();
