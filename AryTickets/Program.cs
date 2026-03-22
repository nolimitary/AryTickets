using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

// Database configuration — Railway provides DATABASE_URL for PostgreSQL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse Railway's DATABASE_URL (postgres://user:pass@host:port/db)
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var npgsqlConn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(npgsqlConn));
}
else if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
{
    // SQLite for local development
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=AryTixDb.db"));
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

// Apply migrations and seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // EnsureCreated may skip if __EFMigrationsHistory exists from a failed deploy
    if (!db.Database.EnsureCreated())
    {
        try
        {
            var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            creator.CreateTables();
        }
        catch { /* Tables already exist — that's fine */ }
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

    // Seed showtimes from TMDb if none exist
    if (!db.Showtimes.Any())
    {
        var tmdbKey = builder.Configuration["TMDb:ApiKey"];
        if (!string.IsNullOrEmpty(tmdbKey) && tmdbKey != "YOUR_KEY_HERE")
        {
            try
            {
                var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
                var url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={tmdbKey}&language=en-US&page=1&region=US";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<AryTickets.Models.ApiResult>(json);
                    var movies = (result?.Results ?? new List<AryTickets.Models.Movie>())
                        .Where(m => !string.IsNullOrEmpty(m.PosterPath) && m.VoteAverage >= 5.0)
                        .Take(10)
                        .ToList();

                    var halls = new[] { "Hall 1", "Hall 2", "Hall 3" };
                    var prices = new[] { 10.50m, 12.50m, 14.00m, 16.00m };
                    var timeSlots = new[] {
                        new TimeSpan(11, 0, 0),  // 11:00 AM
                        new TimeSpan(14, 30, 0), // 2:30 PM
                        new TimeSpan(17, 0, 0),  // 5:00 PM
                        new TimeSpan(19, 30, 0), // 7:30 PM
                        new TimeSpan(21, 45, 0), // 9:45 PM
                    };

                    var rng = new Random(42); // fixed seed for consistency

                    foreach (var movie in movies)
                    {
                        // Each movie gets showtimes across the next 7 days
                        var daysToSchedule = rng.Next(3, 8); // 3-7 days
                        var movieHall = halls[rng.Next(halls.Length)];
                        var moviePrice = prices[rng.Next(prices.Length)];

                        // Pick 2-4 time slots for this movie
                        var slotCount = rng.Next(2, 5);
                        var movieSlots = timeSlots.OrderBy(_ => rng.Next()).Take(slotCount).OrderBy(t => t).ToArray();

                        for (int day = 0; day < daysToSchedule; day++)
                        {
                            var date = DateTime.UtcNow.Date.AddDays(day);
                            foreach (var slot in movieSlots)
                            {
                                // Skip some slots randomly for variety
                                if (day > 0 && rng.Next(10) < 2) continue;

                                db.Showtimes.Add(new AryTickets.Models.Showtime
                                {
                                    TmdbMovieId = movie.Id,
                                    MovieTitle = movie.Title,
                                    PosterPath = movie.PosterPath,
                                    ShowDateTime = date.Add(slot),
                                    Hall = movieHall,
                                    Price = moviePrice,
                                    IsActive = true
                                });
                            }
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch
            {
                // Seeding failed — not critical, admin can add manually
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseResponseCompression();

// Only redirect to HTTPS in local dev — Railway handles TLS at the proxy
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Trust Railway's reverse proxy headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Security headers
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

// Map SignalR hub
app.MapHub<SeatHub>("/hubs/seats");

app.Run();
