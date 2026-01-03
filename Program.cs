using System.Globalization;
using auth.Data;
using auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using QuestPDF;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license (Community) to allow PDF generation in development
QuestPDF.Settings.License = LicenseType.Community;

// Force invariant culture for model binding (dot decimal separator)
var culture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity (Users + Roles)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// =========================
// Custom services (DI)
// =========================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ManagerParkingService>();
builder.Services.AddScoped<UserParkingService>();

var app = builder.Build();

// =========================
// Stripe init (SAFE)
// =========================
var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrWhiteSpace(stripeKey) || stripeKey.Contains("xxx"))
{
    throw new InvalidOperationException(
        "Stripe SecretKey invalide. Mets une vraie clé dans appsettings.json : Stripe:SecretKey (ex: sk_test_...)."
    );
}
StripeConfiguration.ApiKey = stripeKey;

// =========================
// DB init: migrations + roles + SuperAdmin
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var authService = services.GetRequiredService<AuthService>();

        // Apply migrations
        context.Database.Migrate();

        // Create roles + SuperAdmin
        await authService.InitializeRolesAndAdminAsync();

        // Ensure ContactMessages table exists (fallback when migrations not applied)
        var ensureSql = @"IF OBJECT_ID(N'dbo.ContactMessages', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContactMessages](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [Subject] NVARCHAR(120) NOT NULL,
        [Message] NVARCHAR(2000) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
        [IsRead] BIT NOT NULL DEFAULT(0)
    )
END";

        await context.Database.ExecuteSqlRawAsync(ensureSql);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// =========================
// HTTP pipeline
// =========================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
