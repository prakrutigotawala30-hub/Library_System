using Library_Management_System.Models;
using Library_Management_System.Services;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// DATABASE
// Provider order: appsettings override -> SqlServer on Windows -> Sqlite elsewhere
// (Mac/Linux). LocalDB doesn't exist outside Windows, so falling through to
// Sqlite keeps the app runnable on macOS/Linux dev boxes.
var configuredProvider = builder.Configuration.GetValue<string>("Database:Provider");
var dbProvider = !string.IsNullOrWhiteSpace(configuredProvider)
    ? configuredProvider
    : (OperatingSystem.IsWindows() ? "SqlServer" : "Sqlite");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        // Absolute path to the SHARED Sqlite file at the solution root.
        // ContentRootPath is the project directory at runtime; ".." goes up
        // one level into the solution root where both apps can find the same
        // file. Computing this absolutely (instead of using a relative
        // "../LibraryManagementDB.db") removes every working-directory
        // ambiguity — `dotnet run`, F5 from VS Code, IIS Express all resolve
        // to the same physical file.
        //
        // Pooling=False is critical for the dev cross-app scenario: with
        // pooling on, EF holds connections that may hold an old WAL snapshot
        // — so the user app's read can miss the admin's recent write until
        // the connection is recycled. With pooling off, every query opens a
        // fresh connection and sees the latest committed state immediately.
        //
        // Cache=Shared lets multiple connections in the same process share
        // the page cache, reducing disk reads.
        var sqliteFile = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, "..", "LibraryManagementDB.db"));

        var sqliteConn = $"Data Source={sqliteFile};Cache=Shared;Pooling=False";

        options.UseSqlite(sqliteConn);

        // Runs PRAGMA journal_mode=WAL + busy_timeout=5000 on every Sqlite
        // connection EF opens.
        options.AddInterceptors(new SqlitePragmaInterceptor());
    }
    else
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.CommandTimeout(120));
    }
});

// IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// COOKIE SETTINGS
// Distinct cookie name so this user app's session never collides with the
// sibling admin app (LibraryManagementSystem) if both are ever hosted on the
// same domain. Default Identity name is `.AspNetCore.Identity.Application`
// — that would cause the two apps to fight over the same cookie.

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".LibraryUser.Auth";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddControllersWithViews();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
    // HTTPS redirect only in non-dev. In Development a fresh Mac/Linux clone
    // typically has no trusted dev cert (`dotnet dev-certs https --trust`),
    // and unconditionally redirecting to https would either prompt cert
    // warnings or break the request entirely. Production should always have
    // a real cert, so redirect is correct there.
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    // Create / migrate schema. Migrations target SqlServer; on Sqlite we
    // build the schema directly from the EF model so Mac/Linux dev runs
    // get every table the project defines without needing migration files
    // generated for Sqlite. This block was previously missing entirely —
    // that's why no tables appeared on Mac.
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();

        // Concurrency fix: default Sqlite journal mode locks the whole file
        // for writers, which makes the admin app's SaveChanges throw
        // "database is locked" whenever the user app has the .db open.
        // WAL allows one writer + concurrent readers, and busy_timeout
        // tells Sqlite to wait up to 5s for a lock to clear before failing.
        // journal_mode is persisted in the .db file once set.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;");
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    var roleManager =
        scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Member"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }
}

app.Run();