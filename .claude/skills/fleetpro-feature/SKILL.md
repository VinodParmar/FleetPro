---
name: fleetpro-feature
description: Implement or change features in the FleetPro multi-tenant fleet management app (ASP.NET Core MVC, .NET 10, EF Core 9, SQL Server). Use when adding a new module/entity/screen, adding fields to an existing entity, adding permissions or menu items, fixing tenant/SuperAdmin bugs, or touching Services.cs / Controllers.cs / Views in src/FleetPro.Web.
---

# FleetPro feature implementation

FleetPro is a **multi-tenant** (one tenant = one transport company) fleet app: trucks, drivers,
agents (brokers), trips (UP/DOWN phases + payments), expenses, alerts, reports, audit log.
Single project: `src/FleetPro.Web` (namespace root `FleetPro`). UI is AdminLTE/Bootstrap 4 +
Font Awesome, bilingual English/Hindi.

Read this whole file before writing code. Follow the existing patterns — do not introduce
new architecture (no repositories, MediatR, AutoMapper, Identity, separate projects).

## 1. Where things live

| Concern | File | Notes |
|---|---|---|
| Entities | `Models/Entities/FleetEntities.cs`, `UserAndRole.cs`, `Tenant.cs`, `BaseEntity.cs` | Enums live next to entities |
| View models | `Models/ViewModels/ViewModels.cs` | One file, all VMs, DataAnnotations validation |
| DbContext + mapping | `Data/AppDbContext.cs` | Fluent config per entity in `OnModelCreating` |
| Seed (permissions, roles, menu, sample data) | `Data/DataSeeder.cs` | Runs at startup from `Program.cs` |
| Audit | `Data/AuditInterceptor.cs` | Automatic on SaveChanges — don't log CRUD manually |
| Services (all) | `Services/Services.cs` | Interface + class side by side, separated by `// ═══` banners |
| File uploads | `Services/StorageService.cs` | Use `IStorageService`, config in `Storage` section |
| Controllers (all) | `Controllers/Controllers.cs` | Inherit `BaseController` |
| Base controller | `Controllers/BaseController.cs` | `EId/DId`, `CheckPermission`, alert count |
| Views | `Views/<Controller>/Index.cshtml`, `Create.cshtml` (Create doubles as Edit) | Shared partials in `Views/Shared/_*.cshtml` |
| Localization | `Resources/SharedResource.resx` (en) + `SharedResource.hi.resx` (hi) | Injected as `L` |
| DI registration | `Program.cs` | `AddScoped<IXService, XService>()` |
| DB schema (source of truth) | `src/FleetPro.Web/database/FleetPro_Schema.sql` | **Not** the older copy in repo-root `database/` |

## 2. Core rules (must follow)

### Entities
- Tenant-owned data inherits `TenantBaseEntity` (`TenantId`, `Tenant`); global/master data inherits `BaseEntity`.
- `BaseEntity` gives `Id, CreatedAt, UpdatedAt, IsDeleted, CreatedBy, UpdatedBy`.
- **Soft delete only**: set `IsDeleted = true`; never `Remove()` business rows.
- Every entity config in `AppDbContext` adds `e.HasQueryFilter(x => !x.IsDeleted)`, an explicit `ToTable("Plural")`, `decimal` columns with `HasColumnType("decimal(p,s)")`, and FKs to `Tenant` with `OnDelete(DeleteBehavior.Restrict)`.
- Computed values (e.g. `Trip.StartDate`, `TotalExpenses`) are unmapped — they can't be used in LINQ-to-SQL `Where`; load then filter in memory (see `TripService.GetAllAsync`).

### Database changes
The database is created/updated by **SQL scripts**, not by running EF migrations
(`PendingModelChangesWarning` is suppressed in `Program.cs`). For any schema change:
1. Update the entity + `AppDbContext` mapping.
2. Add the `CREATE TABLE` / `ALTER TABLE` to `src/FleetPro.Web/database/FleetPro_Schema.sql`
   in the same style (`INT IDENTITY(1,1) PRIMARY KEY`, `TenantId INT NOT NULL`, audit columns,
   `IsDeleted BIT NOT NULL DEFAULT 0`, named `FK_`/`UQ_` constraints, filtered index
   `WHERE IsDeleted=0` on `TenantId`).
3. Give the user a separate idempotent `ALTER` script (guarded by `IF COL_LENGTH(...) IS NULL` /
   `IF OBJECT_ID(...) IS NULL`) to run on existing databases. Do not run it against their DB
   without asking.
4. Optionally add an EF migration (`dotnet ef migrations add <Name>`) to keep the snapshot in sync,
   but never rely on it for deployment.

### Multi-tenancy & SuperAdmin (most common source of bugs)
- `ICurrentTenantService` (`_current`): `TenantId` (null for SuperAdmin), `UserId`, `IsSuperAdmin`, `UserRole`, `HasPermission(key)`.
- **SuperAdmin has no TenantId.** Never write `_current.TenantId!.Value` or `_current.TenantId ?? 0` on a path SuperAdmin can reach.
- Service `Query()` pattern — SuperAdmin sees everything (with `.Include(x => x.Tenant)` for the Company column), others are filtered:
  ```csharp
  private IQueryable<Truck> Query() =>
      _current.IsSuperAdmin
          ? _db.Trucks.Include(t => t.Tenant)
          : _db.Trucks.Where(t => t.TenantId == _current.TenantId);
  ```
  `GetByIdAsync` must go through `Query()` so tenants can't read other tenants' rows.
- Deciding TenantId on create, in priority order:
  1. **Derive from a parent record** when one exists (Trip → Truck's tenant; TripPhase/TripPayment/TripDocument/Expense-on-trip → Trip's tenant). Validate related records share the tenant.
  2. Otherwise SuperAdmin picks it: `TenantId` on the VM + `ViewBag.Tenants` dropdown rendered only for SuperAdmin (see `TruckController.PopT()` and `Views/Truck/Create.cshtml` "Company *").
  3. Non-SuperAdmin: always `_current.TenantId.Value`, and reject a VM TenantId that differs.
- In the service `CreateAsync`, validate the tenant exists and throw `InvalidOperationException` with a user-readable message; the controller catches it and calls `ModelState.AddModelError(string.Empty, ex.Message)` and re-renders the form (see `TripController.Create`).
- Tenant-scoped numbering (e.g. `GenerateTripNumberAsync(int tenantId)`) takes the tenant as a parameter — never reads `_current`.

### Permissions & roles
- Keys are `module.action`, lowercase: `trucks.view`, `trips.attachdocument`.
- New module → add a tuple to the `modules` array in `DataSeeder.SeedPermissionsAsync`, then decide defaults for roles in `SeedRolesAsync`: `SuperAdmin` (all), `TenantAdmin` (all within tenant), `DataEntryOperator` (view/create/edit, no delete), `Viewer` (view only). Seeding is additive/idempotent — keep it that way.
- Every mutating action starts with:
  ```csharp
  if (CheckPermission("trucks.create") is { } r) return r;
  ```
- SuperAdmin-only controllers use `[Authorize(Roles="SuperAdmin")]`; otherwise `[Authorize]` + `CheckPermission`.

### Controllers
- Add to `Controllers/Controllers.cs` under a `// MODULE` comment, inheriting `BaseController`; constructor `(IXService svc, AppDbContext db, ICurrentTenantService c) : base(db, c)`.
- **IDs in routes are encrypted**: actions take `string eid`, decode with `var id = DId(eid); if (id == 0) return NotFound();`; links/redirects use `EId(id)` (controller) or `Ids.Protect(id)` (views). Never expose raw int IDs in URLs.
- Standard actions: `Index(filters…)`, `Create()` GET/POST, `Edit(string eid)` GET/POST rendering `View("Create", vm)`, `Delete(string eid)` POST. Use `[HttpPost, ValidateAntiForgeryToken]` (CSRF is also global).
- On invalid model: repopulate dropdowns (`await PopX();`) and `return View(vm)`.
- Success feedback: `TempData["Success"] = "..."`; errors: `TempData["Error"]`.
- Map VM ↔ entity with private `ToEntity(vm)` / static `ToVM(entity)` helpers in the controller.
- Dropdowns go in `ViewBag` as `SelectList`, loaded in a private `PopX()` method.

### Services
- Add interface + implementation in `Services/Services.cs` under a banner; register in `Program.cs` as Scoped.
- Constructor `(AppDbContext db, ICurrentTenantService current)`.
- `UpdateAsync` sets `UpdatedAt = DateTime.UtcNow`; `DeleteAsync` soft-deletes.
- Use `IgnoreQueryFilters()` only when you deliberately need soft-deleted rows (e.g. number sequences).
- Log with injected `ILogger<T>` using `[TAG]` prefixes (e.g. `[AUTH]`), Serilog writes to `logs/fleetpro-*.log`.

### Views
- Start with the banner comment `@* ═══ Views/X/Y.cshtml ═══ *@`, then `@model`, then set `ViewData["Title"] = L["..."].Value`.
- Layout: `content-header` with `h1` + breadcrumb (Dashboard home icon → module), then `<section class="content"><div class="container-fluid">` with AdminLTE `card`s. Copy structure from `Views/Truck/Index.cshtml` and `Views/Truck/Create.cshtml`.
- **Inside `<script>`, never write `'@L["Text"]'`** — Razor HTML-encodes it and Hindi shows as `&#x916;…`. Use `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(L["Text"].Value))` (no surrounding quotes). In HTML attributes (`title=`, `placeholder=`) plain `@L[...]` is fine.
- **All user-visible text through `@L["English text"]`**, and add the key to both `.resx` files (`SharedResource.resx` value = English, `SharedResource.hi.resx` value = Hindi). Format: `<data name="Key" xml:space="preserve"><value>…</value></data>`.
- Forms: `asp-route-eid="@Ids.Protect(Model.Id)"`, `@Html.AntiForgeryToken()`, `<div asp-validation-summary="ModelOnly" class="alert alert-danger">`, per-field `asp-validation-for`.
- Show a "Company" column / picker only when `User.IsInRole("SuperAdmin")`.
- Reusable modals/partials go in `Views/Shared/_Name.cshtml` (see `_ExpenseList`, `_PhaseForm`, `_PaymentLedger`).

### Menu
- Sidebar is DB-driven (`MenuItems` table, rendered by `IMenuService` → `_Sidebar.cshtml`).
- New screen → add a `MenuItem` in `DataSeeder.SeedMenuItemsAsync` **and** an `INSERT` in `database/SeedMenuItems.sql` (the seeder skips if any menu rows exist, so existing DBs need the SQL). Set `RequiredPermission`, `SuperAdminOnly` / `TenantAdminOrAbove`, `Icon` (`fas fa-…`), `SortOrder`, `ParentId` (group). `Title` is also the localization key — add it to both resx files.
- Use `OnClick` instead of Controller/Action to open a modal (e.g. `openAddExpenseModal()`).

## 3. New-module checklist

1. Entity in `Models/Entities` (TenantBaseEntity or BaseEntity) + enum(s).
2. `DbSet` + fluent config (table, filter, decimals, FKs, indexes) in `AppDbContext`.
3. SQL in `src/FleetPro.Web/database/FleetPro_Schema.sql` + idempotent upgrade script for the user.
4. ViewModel with validation in `ViewModels.cs` (include `int? TenantId`, `string? TenantName` if tenant-owned).
5. Service interface + class (`Query()` tenant pattern, create validation) → register in `Program.cs`.
6. Permissions in `DataSeeder` (+ role defaults).
7. Controller with `CheckPermission`, `eid` routes, `PopX()` dropdowns, SuperAdmin tenant handling.
8. Views `Index.cshtml` + `Create.cshtml` following the Truck views.
9. Menu item (seeder + `SeedMenuItems.sql`).
10. Localization keys in **both** resx files.
11. Build and verify (below).

## 4. Build, run, verify

```powershell
cd src/FleetPro.Web
dotnet build          # if it fails with "file is locked by FleetPro.Web", the app is running — stop it first
dotnet run            # https://localhost:58789  /  http://localhost:58790
```
- `ASPNETCORE_ENVIRONMENT=Development` (set by `launchSettings.json`) uses the local DB in
  `appsettings.Development.json`; without it, `appsettings.json` (remote) is used. If startup
  fails at `DataSeeder` with "Database not found", check `logs/fleetpro-*.log` for which
  connection string was resolved (`[DB-CONFIG]` line) before blaming the schema.
- There is no test project. Verify by building, then exercising the flow as **both** a SuperAdmin
  and a tenant user (TenantAdmin) — most regressions are SuperAdmin-has-no-tenant or
  cross-tenant leaks.
- Never commit connection-string passwords or put real credentials in new config.

## 5. Known pitfalls

- `_current.TenantId!.Value` / `?? 0` on SuperAdmin paths → `InvalidOperationException` or rows with TenantId 0.
- Forgetting `HasQueryFilter(!IsDeleted)` → soft-deleted rows reappear.
- Filtering on unmapped computed properties in SQL → EF translation exception.
- Adding a resx key in only one language → Hindi UI shows the English key.
- Adding a menu item only in the seeder → invisible on existing databases.
- Raw int IDs in URLs → break `DId()` (returns 0 → 404) and leak IDs.
- Two `FleetPro_Schema.sql` files exist; edit the one under `src/FleetPro.Web/database/`.
