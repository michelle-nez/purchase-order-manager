# Troubleshooting

Problems you can actually hit with this application, what causes each one, and how to
confirm the fix.

Almost every first-run problem is one of two things: **the connection string is not
set**, or **the migration has not been applied**.

## Setup and startup

### The app builds and starts, but any page touching data fails

Usually the connection string is missing. `appsettings.json` ships
`"PurchasingDatabase": ""` and nothing supplies a value until you set a User Secret.

```bash
dotnet user-secrets list --project PurchaseOrders.Web
```

Expect `ConnectionStrings:PurchasingDatabase = Server=(localdb)\...`. Nothing listed
means no secret is set — see
[getting-started.md](getting-started.md#2-add-the-connection-string).

### "Cannot open database 'PurchaseOrderManager' requested by the login"

The connection string is fine; the database does not exist yet. Nothing creates it.

```bash
dotnet ef database update --project PurchaseOrders.Data --startup-project PurchaseOrders.Web
```

### I edited the connection string in appsettings.json and nothing changed

**User Secrets override `appsettings.json`.** In Development the secret wins. Change
the secret instead, and leave the committed file empty.

### "Unable to create an object of type 'PurchasingDbContext'"

The EF Core tools are running against the wrong startup project. Migrations live in
`PurchaseOrders.Data`, but the tools and the connection string live in
`PurchaseOrders.Web`, so both must be named:

```bash
dotnet ef database update --project PurchaseOrders.Data --startup-project PurchaseOrders.Web
```

In Package Manager Console: **Default project** = `PurchaseOrders.Data`, **solution
startup project** = `PurchaseOrders.Web`.

### "dotnet ef does not exist" / not recognized

```bash
dotnet tool install --global dotnet-ef
```

### The app will not start — "cannot run a class library"

`PurchaseOrders.Data` has been set as the startup project. It has no entry point.
Right-click **`PurchaseOrders.Web`** → **Set as Startup Project**.

### Port already in use

The launch profiles bind 5126 (http) and 7145 (https). Check for a stray `dotnet`
process, or edit `PurchaseOrders.Web/Properties/launchSettings.json`.

## Totals and receipts

### An order's total shows $0.00 even though it has lines

**The lines were not loaded.** `Total` is a derived property that sums an in-memory
collection, so an order fetched without `.Include(o => o.Lines)` reports zero rather
than throwing.

```csharp
order = await db.PurchaseOrders
    .Include(o => o.Lines)          // <- required for any total
    .FirstOrDefaultAsync(o => o.Id == Id);
```

This is the most likely bug to introduce when adding a new screen. Fully explained in
[database.md](database.md#the-cost-of-deriving).

### A migration tries to create a Total column, or one for Outstanding

A derived property was added without telling EF Core to ignore it. Every computed
member needs a line in `OnModelCreating`:

```csharp
modelBuilder.Entity<PurchaseOrder>().Ignore(p => p.Total);
```

Without it EF Core treats the expression as a mapped column. Delete the bad migration,
add the `Ignore`, and generate it again.

### Receiving says "Only N outstanding … received that many"

**Working as designed.** You asked to receive more than the line has outstanding, so
the app took what was left and told you. `Math.Min(wanted, line.Outstanding)` runs on
the server, so editing the input's `max` in browser dev tools changes nothing.

### "Enter how many arrived before receiving a line"

The quantity was zero or negative. The box defaults to the line's outstanding
quantity, so this usually means it was cleared or a fully received line was retried —
a complete line has zero outstanding, so there is nothing left to receive.

### The status is wrong after receiving

Status is derived, not stored, so it cannot be out of date — it is recomputed from the
lines every time the page loads. If it looks wrong, the quantities are what is wrong:

```sql
SELECT Sku, QuantityOrdered, QuantityReceived
FROM PurchaseOrderLines WHERE PurchaseOrderId = <id>;
```

| State | Condition |
|---|---|
| No lines | the order has no lines |
| Awaiting | nothing received on any line |
| Part received | some received, less than ordered overall |
| Complete | received reaches or exceeds ordered |

### Two deliveries were received at once and one vanished

Expected with the current schema. There is no concurrency token, so two people
receiving the same line simultaneously both read the same starting quantity and the
second save wins. Noted under
[architecture.md → Recommendations](architecture.md#recommendations).

## Data and EF Core

### The supplier dropdown is empty

Seed data has not been applied. The four suppliers are inserted by the
`InitialCreate` migration, not at runtime, so an empty dropdown usually means the
migration did not run — or ran against a different database than the app is using.

### "PO number 'X' is already in use" — but that order is not in the list

**Expected behavior, not a bug.** Cancelling an order is a soft delete: the row stays
and keeps its PO number, and the unique index still covers it.

```sql
SELECT Id, PoNumber, IsCancelled FROM PurchaseOrders WHERE PoNumber = 'X';
```

An `IsCancelled = 1` row is the culprit.

### "Could not save this purchase order. Please try again."

The generic save failure on the header — the save threw something that was **not** a
duplicate PO number. The real exception is logged: look for
`Failed to save purchase order <PoNumber>` in the console output.

### Adding or removing a line throws instead of showing a message

**A known gap.** `AddLineAsync` and `RemoveLineAsync` have no `DbUpdateException`
handler, unlike the header save path, so a database failure there surfaces as an
unhandled exception. Listed under
[architecture.md → Recommendations](architecture.md#recommendations).

### A supplier cannot be deleted

Working as designed. `SupplierId` uses `DeleteBehavior.Restrict`, so a supplier with
orders against it is protected.

### Deleting an order also deleted its lines

Working as designed, and the one place cascade is correct in this app. A line has no
meaning without its order, so orphaning it would be worse. See
[database.md](database.md#relationships-and-delete-behavior).

### "A second operation was started on this context instance"

Should not happen — the app uses `AddDbContextFactory` and each operation opens its
own context. If it appears, something has been changed to `AddDbContext`, or a context
is being held across `await` boundaries. The correct pattern is:

```csharp
await using var db = await DbFactory.CreateDbContextAsync();
```

## UI and runtime

### The page heading is hidden under the app bar

A `pt-*` class has been added to `MudMainContent`, overriding the padding MudBlazor
uses to clear the fixed app bar. Put spacing on the inner `MudContainer` instead.

### MudBlazor components render unstyled, or dialogs never appear

One of the required pieces is missing:

- `builder.Services.AddMudServices()` in `Program.cs`
- `@using MudBlazor` in `Components/_Imports.razor`
- `MudBlazor.min.css` and `MudBlazor.min.js` linked in `App.razor`
- `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider` and
  `MudSnackbarProvider` at the top of `MainLayout.razor`

Missing providers is the usual one: components render, but overlays silently do
nothing.

### "Rejoining the server..." keeps appearing

The Blazor Server circuit dropped. Normal after a restart during debugging — reload.
If it happens while idle, something is interrupting the WebSocket: a proxy, a VPN, or
a firewall.

### An unknown order id shows a blank page

It should not — both `PurchaseOrderDetail` and `PurchaseOrderEdit` redirect to
`/purchase-orders` when the id matches no row. If a blank page appears, check
`BlazorDisableThrowNavigationException` is still `true` in the csproj; the redirects
happen inside lifecycle methods and depend on it.

## Things people look for that are not here

- **Swagger / OpenAPI** — none. No HTTP API, no controllers. `/swagger` will 404.
- **A login page** — no authentication. Every page is public by design.
- **A cancelled-orders screen** — not built; cancelled rows are only visible in SQL.
- **Receipt history** — `QuantityReceived` is a running total, so individual
  deliveries are not recorded.
- **A `Total` column in the database** — deliberately absent. See
  [database.md](database.md#the-design-decision-that-shapes-everything-no-stored-total).

## Still stuck?

Work through the eight checks at the end of
[getting-started.md](getting-started.md#5-check-it-actually-works). They walk the whole
master-detail journey and isolate the failure in a couple of minutes.
