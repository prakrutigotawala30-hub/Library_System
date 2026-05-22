using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SERVICES

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PdfReceiptService>();
builder.Services.AddScoped<ExportService>();

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

        // Diagnostic: log the resolved Sqlite path so it's visible in the
        // dotnet run terminal. Both admin and user apps should print the
        // SAME absolute path. If they don't, the DB is being split.
        Console.WriteLine($"[Admin App] Sqlite database file: {sqliteFile}");
        Console.WriteLine($"[Admin App] Connection string:    {sqliteConn}");

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
    options.SignIn.RequireConfirmedEmail = false;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// COOKIE SETTINGS
// Distinct cookie name so this admin app's session never collides with the
// sibling user app (Library_Management_System) if both are ever hosted on
// the same domain. Default Identity name is `.AspNetCore.Identity.Application`
// — that would cause the two apps to fight over the same cookie.

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".LibraryAdmin.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// ERROR HANDLING

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error500");
    app.UseHsts();
    // HTTPS redirect only in non-dev. In Development a fresh Mac/Linux clone
    // typically has no trusted dev cert (`dotnet dev-certs https --trust`),
    // and unconditionally redirecting to https would either prompt cert
    // warnings or break the request entirely. Production should always have
    // a real cert, so redirect is correct there.
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Home/Error404");

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// ROUTES

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");


// DATABASE + ROLES

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    // Migrations are SqlServer-specific (see AppDbContextModelSnapshot —
    // uses SqlServer identity columns). On Sqlite (Mac/Linux dev) we
    // bypass migrations and build the schema directly from the EF model
    // so every table the project defines appears in the .db file.
    if (db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();

        // Concurrency fix: see SqlitePragmaInterceptor for details.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;");

        // Schema patch: EnsureCreated is one-shot — if the .db already had
        // any tables, EnsureCreated did nothing, so new tables added to the
        // model later won't appear. Patch fills in missing tables/columns.
        await SqliteSchemaPatcher.PatchAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    // Idempotent default data — only seeds tables that are empty.
    // Lets a fresh clone show Books / Authors / Categories / Events on the
    // user-facing pages immediately, before the admin has added anything.
    await DbSeeder.SeedAsync(db);

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Admin",
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