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
        // Path is relative to the project folder. "../LibraryManagementDB.db"
        // points to the SOLUTION ROOT, so both admin and user apps share one
        // file on Mac/Linux. Without this, admin-entered books wouldn't show
        // on the user-facing app because each project wrote to its own .db.
        options.UseSqlite(
            builder.Configuration.GetConnectionString("SqliteConnection")
            ?? "Data Source=../LibraryManagementDB.db");
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
    }
    else
    {
        await db.Database.MigrateAsync();
    }

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