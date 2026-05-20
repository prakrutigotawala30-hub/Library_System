# Library System

Two ASP.NET Core MVC apps sharing one class library:

- **`LibraryManagementSystem/`** — Admin app (books, members, borrows, reports, events CRUD)
- **`Library_Management_System/`** — Member-facing app (catalog, events, wishlist, account)
- **`LibraryManagementSystem.ClassLibrary/`** — Shared `AppDbContext`, models, migrations

Both run cross-platform: SQL Server LocalDB on Windows, SQLite on Mac/Linux.

---

## Quickstart — Mac / Linux (VS Code)

```bash
git clone <repo-url>
cd Library_System

# Admin app
cd LibraryManagementSystem
dotnet run

# OR User app (in a new terminal)
cd Library_Management_System
dotnet run
```

That's it. No setup steps. On first run the app:

1. Detects the OS → picks **SQLite** automatically (no LocalDB on Mac).
2. Creates `LibraryManagementDB.db` at the **solution root** (shared by both apps).
3. Builds the full schema from the EF model — every table the project defines.
4. Seeds `Admin` and `Member` roles.

Browser opens at **http://localhost:5221** (admin) or **http://localhost:5255** (user).

### View the SQLite tables

Open `LibraryManagementDB.db` in any SQLite browser:
- **DB Browser for SQLite** (free, https://sqlitebrowser.org)
- **TablePlus**, **DBeaver**, **JetBrains DataGrip**

The file is created **once at the solution root** — both apps point at it via `../LibraryManagementDB.db`:

```
Library_System/
  LibraryManagementDB.db                  ← shared SQLite file (single source of truth)
  LibraryManagementSystem/                ← admin app
  Library_Management_System/              ← user app
```

This way, when an admin adds a book in the admin app, it shows up immediately in the user-facing catalog. Same behavior as Windows + LocalDB where both apps already share the `LibraryManagementDB` database.

---

## Quickstart — Windows (Visual Studio or VS Code)

```powershell
git clone <repo-url>
cd Library_System
```

Either:
- Open `LibrarySystem.sln` in **Visual Studio 2022+** → set startup project → F5
- Or terminal: `cd LibraryManagementSystem ; dotnet run`

On first run the app:
1. Detects the OS → picks **SQL Server (LocalDB)**.
2. Creates the `LibraryManagementDB` database via `dotnet ef`-generated migrations.
3. Seeds roles.

Both apps share the same LocalDB database on Windows.

---

## First user

Open the admin app → `/Account/Register` → fill the form. The first registered user is granted the **Admin** role automatically (see `LibraryManagementSystem/Controllers/AccountController.cs`). After registering, log in via `/Account/Login`.

The user app at `/Account/Register` creates **Member**-role accounts (for the catalog/wishlist features).

---

## Switching the DB provider explicitly

`appsettings.json` (both projects) has a hint:

```json
"Database": { "Provider": "" }
```

Leave blank for automatic OS-based selection. To force a provider on any OS:

```json
"Database": { "Provider": "Sqlite" }   // or "SqlServer"
```

---

## When the schema changes (model edits)

On **Mac/Linux (SQLite)**: `EnsureCreated` only creates tables — it does **not** update an existing DB after a model change. Delete the shared file at the solution root and run again:

```bash
rm LibraryManagementDB.db
dotnet run
```

On **Windows (SQL Server)**: use migrations as normal:

```powershell
dotnet ef migrations add YourChange --project LibraryManagementSystem.ClassLibrary --startup-project LibraryManagementSystem
dotnet ef database update --project LibraryManagementSystem.ClassLibrary --startup-project LibraryManagementSystem
```

Or from inside the class library (the `AppDbContextFactory` lets EF tools find the context):

```powershell
cd LibraryManagementSystem.ClassLibrary
dotnet ef migrations add YourChange
dotnet ef database update
```

> Migrations target SQL Server (the snapshot uses `SqlServerModelBuilderExtensions.UseIdentityColumns`). Generate them on Windows. Mac/Linux dev does not need migration files — `EnsureCreated` handles dev schema.

---

## Email (password reset / welcome)

`EmailSettings` in `Library_Management_System/appsettings.json` ships with **empty placeholders**. Don't paste credentials into that file (it's in the public repo). Use **user-secrets** on your dev machine:

```bash
cd Library_Management_System
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:SenderEmail" "you@gmail.com"
dotnet user-secrets set "EmailSettings:Username"    "you@gmail.com"
dotnet user-secrets set "EmailSettings:Password"    "<gmail-app-password>"
```

In production, use environment variables (`EmailSettings__Password=…`) or Azure Key Vault.

If credentials are missing, registration still succeeds — only the welcome email skips. Login is never blocked.

---

## HTTPS in dev

HTTPS redirect is **disabled in Development** to keep first-run smooth on Mac/Linux (where the dev cert isn't trusted by default). Use `http://` URLs locally. In Production the redirect is enabled.

If you want HTTPS in dev anyway:
```bash
dotnet dev-certs https --trust    # Windows / Mac: requires sudo on Mac
```
Then launch with the `https` profile from `Properties/launchSettings.json`.

---

## Build everything

```bash
dotnet build LibrarySystem.sln
```

---

## Adding new things — cross-platform rules

The project is dual-target (Mac and Windows). A few habits keep it that way.

### 1. New entity / model

1. Add the C# class in `LibraryManagementSystem.ClassLibrary/Models/`.
2. Add `public DbSet<YourEntity> YourEntities { get; set; }` to `AppDbContext`.
3. On **Mac/Linux** (Sqlite): delete the shared `.db` file at the solution root and `dotnet run`. `EnsureCreated` rebuilds with the new table.
   ```bash
   rm LibraryManagementDB.db
   dotnet run
   ```
4. On **Windows** (SqlServer): generate a migration and apply.
   ```powershell
   dotnet ef migrations add Add_YourEntity --project LibraryManagementSystem.ClassLibrary --startup-project LibraryManagementSystem
   dotnet ef database update --project LibraryManagementSystem.ClassLibrary --startup-project LibraryManagementSystem
   ```
5. Commit the new migration files so the rest of the team can update their LocalDB.

> **Tip**: generate migrations on Windows only. Mac/Linux uses `EnsureCreated` and doesn't need them.

### 2. New controller / action

Just create the C# file under `Controllers/`. No platform-specific concern.

**Don't forget** `[Authorize]` (or `[Authorize(Roles="Admin")]`) on any admin-app controller that handles write actions. Currently `BorrowController`, `DepartmentsController`, `EventsController`, `MembershipController`, `ReportsController` all require Admin role — match that pattern for new admin controllers.

### 3. New view (.cshtml)

- File path is case-sensitive on Mac/Linux. `Views/Foo/Index.cshtml` is **not** the same as `Views/foo/Index.cshtml`.
- When referring to layouts/partials, use the **exact** filename case as on disk:
  ```cshtml
  @{ Layout = "~/Views/Shared/_PublicLayout.cshtml"; }
  @await Html.PartialAsync("_BookCard")
  ```

### 4. New JS / CSS / image file — ⚠️ case-sensitivity trap

This is the #1 silent bug: works on Windows, breaks on Mac.

**Rules**:
- Name **all** new web assets in **lowercase**: `my-page.js`, `my-page.css`, `book-cover.png`. No `MyPage.js`.
- Reference them with the **same** case in cshtml:
  ```cshtml
  <link href="~/css/my-page.css" rel="stylesheet" />
  <script src="~/js/my-page.js"></script>
  <img src="~/images/book-cover.png" />
  ```
- If you ever need to rename a file's case (e.g., `Foo.js` → `foo.js`), use `git mv` so the rename is tracked:
  ```bash
  git mv wwwroot/js/Foo.js wwwroot/js/foo-temp.js
  git mv wwwroot/js/foo-temp.js wwwroot/js/foo.js
  git commit -m "lowercase Foo.js → foo.js"
  ```
  The two-step rename works around Windows' case-insensitive filesystem.

### 5. New file I/O code (uploads, exports, etc.)

- Always use `Path.Combine` — never string-concat with `\\` or `/`.
- Get base paths from `IWebHostEnvironment`: `_env.WebRootPath`, `_env.ContentRootPath`.
- Don't hardcode `C:\…` or `/Users/…`.

```csharp
// ✅ good
var path = Path.Combine(_env.WebRootPath, "images", fileName);

// ❌ breaks on Mac
var path = _env.WebRootPath + "\\images\\" + fileName;
```

### 6. New NuGet package

Before adding a package, check it supports both Windows and macOS. Most modern .NET libraries do, but a few platform-specific ones to watch out for:
- `System.Drawing.Common` — Windows-only since .NET 6 unless you take extra steps. Use `ImageSharp` instead for cross-platform image work.
- Anything calling `Win32` APIs natively.

### 7. New SMTP / email

EmailService reads from `IOptions<EmailSettings>`. Add real credentials via user-secrets, **never** in `appsettings.json`:

```bash
cd Library_Management_System
dotnet user-secrets set "EmailSettings:Password" "<gmail-app-password>"
```

---

## Common "it works on my Windows but not Mac" checklist

1. **File reference case** — make sure cshtml refs match disk case exactly.
2. **Sqlite `.db` file stale** — `rm *.db` and re-run after schema changes.
3. **HTTPS dev cert** — we skip HTTPS in dev. If you still hit cert issues, `dotnet dev-certs https --trust` (Mac needs sudo).
4. **Port already in use** — change ports in `Properties/launchSettings.json`.
5. **Migrations missing** — Mac uses `EnsureCreated`, doesn't need migrations. Windows applies them automatically. If a teammate generated a new migration, just `git pull` and run.

