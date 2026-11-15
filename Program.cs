using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusCommunicationSystem.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ---- SQL Server auto-detect: the first server that answers is used ----
var candidates = new List<string>();
var main = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(main)) candidates.Add(main);
candidates.AddRange(builder.Configuration.GetSection("ConnectionCandidates").Get<string[]>() ?? Array.Empty<string>());

string connectionString = null;
foreach (var cs in candidates)
{
    try
    {
        var test = new SqlConnectionStringBuilder(cs) { InitialCatalog = "master", ConnectTimeout = 20 };
        using var conn = new SqlConnection(test.ConnectionString);
        conn.Open();
        connectionString = cs;
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine("SQL Server not reachable with: " + cs + " -> " + ex.Message);
    }
}

if (connectionString == null)
{
    throw new InvalidOperationException(
        "SQL Server nahi mila. Visual Studio Installer me 'SQL Server Express LocalDB' install karo " +
        "ya SQL Server Express install karke appsettings.json me connection string theek karo.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ---- One login cookie for all five portals; every portal checks its own ROLE ----
//      Admin / Customer / Technical / Accounts / Retail
static string LoginPathFor(PathString path)
{
    var p = path.Value ?? "";
    if (p.StartsWith("/Customer", StringComparison.OrdinalIgnoreCase)) return "/Customer/Login";
    if (p.StartsWith("/Technical", StringComparison.OrdinalIgnoreCase)) return "/Technical/Login";
    if (p.StartsWith("/Accounts", StringComparison.OrdinalIgnoreCase)) return "/Accounts/Login";
    if (p.StartsWith("/Retail", StringComparison.OrdinalIgnoreCase)) return "/Retail/Login";
    if (p.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)) return "/Admin/Login";
    return "/";
}

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Send each portal's visitors to THAT portal's login page
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.Redirect(LoginPathFor(ctx.Request.Path));
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.Redirect(LoginPathFor(ctx.Request.Path));
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// ---- database + tables + starter data are created automatically ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // If you later add EF migrations (Add-Migration InitialCreate) they are applied here;
        // until then the tables are created straight from the models.
        if (db.Database.GetMigrations().Any())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();

        DbSeeder.Seed(db);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "Database setup failed: " + ex.GetBaseException().Message +
            "  (Agar purani/adhuri database hai to usay SSMS se delete karke dobara run karo.)", ex);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
