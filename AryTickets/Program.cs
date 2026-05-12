using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.ResponseCompression;
using AryTickets.Data;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Hubs;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Bind to PORT env var (Railway sets this)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Database configuration — Railway provides DATABASE_URL for PostgreSQL,
// otherwise we expect a SQL Server connection string (LocalDB on Windows).
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var npgsqlConn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(npgsqlConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/StatusCode/403";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddTransient<IEmailSender, ResendEmailSender>();
builder.Services.AddTransient<TicketPdfGenerator>();
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// SignalR for real-time seat updates
builder.Services.AddSignalR();

// Response compression for production
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "text/css",
        "application/json"
    });
});

var app = builder.Build();

// Apply migrations and seed admin + repertoire
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var providerName = db.Database.ProviderName ?? "";
    if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.Migrate();
    }
    else
    {
        if (!db.Database.EnsureCreated())
        {
            try
            {
                var creator = db.GetService<IRelationalDatabaseCreator>();
                creator.CreateTables();
            }
            catch { /* tables already exist */ }
        }
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    var adminEmail = "admin@arytix.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "Admin",
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Seed theater repertoire if empty
    if (!db.Productions.Any())
    {
        var productions = TheaterSeedData.GetProductions();
        db.Productions.AddRange(productions);
        await db.SaveChangesAsync();

        var stages = new[] { "Голяма сцена", "Камерна сцена", "Сцена на сатиричния салон" };
        var prices = new[] { 28.00m, 32.00m, 38.00m, 45.00m };
        var timeSlots = new[]
        {
            new TimeSpan(19, 0, 0),
            new TimeSpan(19, 30, 0),
            new TimeSpan(20, 0, 0),
        };
        var rng = new Random(7);

        foreach (var production in db.Productions.ToList())
        {
            var stage = stages[rng.Next(stages.Length)];
            var price = prices[rng.Next(prices.Length)];
            var daysToSchedule = rng.Next(4, 9);

            for (int d = 0; d < daysToSchedule; d++)
            {
                if (rng.Next(10) < 3 && d > 0) continue;
                var date = DateTime.UtcNow.Date.AddDays(d + 1);
                var slot = timeSlots[rng.Next(timeSlots.Length)];

                db.Performances.Add(new Performance
                {
                    ProductionId = production.Id,
                    ShowDateTime = date.Add(slot),
                    Stage = stage,
                    Price = price,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<SeatHub>("/hubs/seats");

app.Run();
