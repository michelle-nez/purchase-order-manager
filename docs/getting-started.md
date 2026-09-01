# Getting started

From a fresh clone to a running app. Roughly ten minutes, most of it waiting for
NuGet.

## Prerequisites

| Requirement | Notes |
|---|---|
| Visual Studio 2026 | With the **ASP.NET and web development** workload |
| .NET 10 SDK | `dotnet --version` should report 10.x |
| SQL Server LocalDB | Ships with the workload. Any SQL Server instance works — only the connection string changes |

Check LocalDB is present:

```powershell
sqllocaldb info
```

`MSSQLLocalDB` should be listed. If not, add **SQL Server Express LocalDB** through
the Visual Studio Installer under Individual components.

## 1. Clone and open

```bash
git clone https://github.com/michelle-nez/purchase-order-manager.git
cd purchase-order-manager
```

Open `PurchaseOrderManager.sln`. `PurchaseOrders.Web` is already the startup project.

## 2. Add the connection string

**The app will not run until this is done.** `appsettings.json` ships the key with an
empty value so the shape is documented without a credential in the repository:

```json
"ConnectionStrings": {
  "PurchasingDatabase": ""
}
```

**In Visual Studio** — right-click `PurchaseOrders.Web` → **Manage User Secrets**:

```json
{
  "ConnectionStrings": {
    "PurchasingDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=PurchaseOrderManager;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Or from the CLI**, run from the solution folder:

```bash
dotnet user-secrets set "ConnectionStrings:PurchasingDatabase" "Server=(localdb)\MSSQLLocalDB;Database=PurchaseOrderManager;Trusted_Connection=True;TrustServerCertificate=True" --project PurchaseOrders.Web
```

`Trusted_Connection=True` means Windows authentication, so there is no password to
store anywhere.

## 3. Create the database

Nothing creates it automatically — there is no `Migrate()` or `EnsureCreated()` call
in `Program.cs`.

**Package Manager Console** — **Default project** `PurchaseOrders.Data`, startup
project `PurchaseOrders.Web`:

```powershell
Update-Database
```

**Or from the CLI**, naming both projects:

```bash
dotnet ef database update --project PurchaseOrders.Data --startup-project PurchaseOrders.Web
```

If `dotnet ef` is not recognized:

```bash
dotnet tool install --global dotnet-ef
```

This creates three tables, three indexes, both foreign keys — one cascading, one
restricted — and inserts four suppliers.

## 4. Run it

Press **F5**, or:

```bash
dotnet run --project PurchaseOrders.Web
```

| Profile | URL |
|---|---|
| `http` | http://localhost:5126 |
| `https` | https://localhost:7145 (also serves 5126) |

Both set `ASPNETCORE_ENVIRONMENT=Development`. On first run over HTTPS:

```bash
dotnet dev-certs https --trust
```

## 5. Check it actually works

A fresh database has **four suppliers and no orders**. The checks below walk the whole
master-detail journey, including the two rules that are easy to get wrong.

1. Open `/purchase-orders` — the empty state, not an error.
2. Click **New order**. The supplier dropdown should list Ultra Spec Cables,
   FiberCables.com, Wallplate City and Nerdom Micro. Save a PO with a number and a
   supplier.
3. You land on the **detail page**, not the list — that is deliberate, since a new
   order has no lines yet.
4. **Add a line**: SKU, description, quantity 10, unit cost 5.00. The summary should
   now read a total of **$50.00** and status **Awaiting**. That total is computed from
   the line, not stored.
5. **Receive part of it** — enter 4 and receive. Status becomes **Part received**,
   outstanding shows 6.
6. **Try to over-receive**: enter 99 and receive. It accepts only the 6 outstanding
   and tells you so. Status becomes **Complete**. This is the server-side clamp — you
   can bypass the input's `max` in browser dev tools and the result is the same.
7. **Add a second order reusing the same PO number.** Rejected with "PO number 'X' is
   already in use." — the unique index doing its job.
8. Back on `/purchase-orders`, the list shows each order's derived total and status.

If all eight behave, the schema, the derived totals and the receive rule are all
wired up correctly.

## Current deployment state

**This application is not deployed anywhere, and has no deployment configuration.**

Verified in the repository: no publish profile (`.pubxml`), no `Dockerfile`, no
`.github/workflows`, no `appsettings.Production.json`. It runs on a developer machine
against LocalDB.

## Optional future deployment

**Nothing here is implemented.**

Because this is Blazor Server: it needs a real .NET host holding a **live SignalR
connection** (static hosts cannot run it), a **reachable SQL Server** rather than
LocalDB, and **sticky sessions** if ever scaled past one instance.

A minimal deployment would need a .NET 10 host, a SQL Server database there with the
connection string supplied through the host's configuration or environment variables
(**never the repository**), the migration applied against it — via
`dotnet ef database update` or a script from `dotnet ef migrations script` — and
`ASPNETCORE_ENVIRONMENT` set to `Production`, which switches on `UseExceptionHandler`
and HSTS.

Applying migrations automatically at startup is deliberately not done; it would let a
deploy alter a production schema without anyone choosing to.

## Where to go next

| Document | Covers |
|---|---|
| [architecture.md](architecture.md) | Projects, layers, where the business rules live |
| [database.md](database.md) | Schema, master-detail, why there is no Total column, ER diagram |
| [configuration.md](configuration.md) | Every setting, User Secrets, environments |
| [troubleshooting.md](troubleshooting.md) | Problems specific to this app |
