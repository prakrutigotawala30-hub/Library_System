using Library_Management_System.Models;
using Library_Management_System.Services;
using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var configuredProvider = builder.Configuration.GetValue<string>("Database:Provider");
var dbProvider = !string.IsNullOrWhiteSpace(configuredProvider)
    ? configuredProvider
    : (OperatingSystem.IsWindows() ? "SqlServer" : "Sqlite");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteFile = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, "..", "LibraryManagementDB.db"));

        var sqliteConn = $"Data Source={sqliteFile};Cache=Shared;Pooling=False";

        Console.WriteLine($"[User App]  Sqlite database file: {sqliteFile}");
        Console.WriteLine($"[User App]  Connection string:    {sqliteConn}");

        options.UseSqlite(sqliteConn);

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
    options.SignIn.RequireConfirmedEmail = true;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

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

        // Concurrency fix: see SqlitePragmaInterceptor for details.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;");
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    // Idempotent default data — only seeds tables that are empty.
    // Lets a fresh clone show Books / Authors / Categories / Events on the
    // user-facing pages immediately, before the admin has added anything.
    await DbSeeder.SeedAsync(db);

    var roleManager =
        scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
{
    "User",
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
