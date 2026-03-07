using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// PayMongo & Email services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PayMongoService>();
builder.Services.AddSingleton<SubscriptionEmailService>();

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
