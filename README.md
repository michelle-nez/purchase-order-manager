# Purchase Order Manager

Supplier purchase orders with line items and partial receipts. Raise a PO, add the
lines you are buying, then receive stock against those lines as it arrives -
including when only part of a line turns up. Built to practice master-detail
relationships and aggregate queries on SQL Server.

![Purchase order detail](screenshots/purchase-order-detail.png)

## Stack

- .NET 10, Blazor Server (interactive server rendering)
- Entity Framework Core 10
- SQL Server (LocalDB in development)

## What it does

- Purchase orders and their lines in a real **master-detail** relationship
- Order totals, quantities, and receipt status are **derived from the lines**
- Partial receipts, with outstanding quantity tracked per line
- Receiving is clamped so received can never exceed ordered, whatever is typed
- Unique index on the PO number, enforced by the database rather than the screen
- Deleting an order cascades to its lines; a supplier in use cannot be deleted
- Responsive - tables become stacked cards on a phone

## The schema

```
dbo.Suppliers
    Id          int, primary key, identity
    Name        nvarchar(120), not null
    Email       nvarchar(200), not null

dbo.PurchaseOrders                    -- the master
    Id           int, primary key, identity
    PoNumber     nvarchar(40), not null, UNIQUE index
    OrderedUtc   datetime2, not null
    ExpectedUtc  datetime2, null
    Notes        nvarchar(300), null
    IsCancelled  bit, not null
    SupplierId   int, not null, foreign key -> Suppliers.Id      (ON DELETE NO ACTION)

dbo.PurchaseOrderLines                -- the detail
    Id                int, primary key, identity
    Sku               nvarchar(40), not null
    Description       nvarchar(200), not null
    QuantityOrdered   int, not null
    QuantityReceived  int, not null
    UnitCost          decimal(18,2), not null
    PurchaseOrderId   int, not null, foreign key -> PurchaseOrders.Id  (ON DELETE CASCADE)
```

## Why there is no Total column

`PurchaseOrders` deliberately has **no `Total` column**. The order total is
`SUM(QuantityOrdered * UnitCost)` across its lines, and the receipt status is worked
out by comparing received quantities against ordered ones.

A stored total is a second copy of something the line items already say. The moment
a line is edited, added, or removed and the total is not recalculated, the order
disagrees with itself - and nothing in the database stops that happening. Deriving
the number means it cannot drift.

The two delete rules follow the same reasoning from the other direction:

- **Lines cascade** when their order is deleted. A line has no meaning without its
  parent, so orphaning it would be worse than removing it.
- **Suppliers are restricted.** A supplier is referenced by orders that still matter,
  so the database refuses the delete rather than silently breaking them.

## Project layout

| Project | What it holds |
|---|---|
| `PurchaseOrders.Web` | Blazor Server app - the screens |
| `PurchaseOrders.Data` | Models, `PurchasingDbContext`, and EF Core migrations |

The reference points one way only: Web references Data, never the reverse. The
context is registered with `AddDbContextFactory`, not `AddDbContext`, because Blazor
Server components are long-lived and several can run at once.

## Running it locally

1. Open `PurchaseOrderManager.sln` in Visual Studio 2026.
2. Right-click `PurchaseOrders.Web` and choose **Manage User Secrets**.
3. Add a `ConnectionStrings:PurchasingDatabase` value pointing at your local SQL
   Server LocalDB instance, with the database named `PurchaseOrderManager` and a
   trusted connection. See `appsettings.json` for the setting's shape.
4. In the Package Manager Console, set **Default project** to `PurchaseOrders.Data`
   and run `Update-Database`.
5. Run the project and open `/purchase-orders`.

The connection string lives in User Secrets only. `appsettings.json` keeps a blank
placeholder so the setting's shape is documented without a value in the repo.

## What I would do next

- Receipt history, so each delivery is its own record rather than a running total
- Cost variance between the ordered unit cost and what was actually invoiced
- Filter by supplier and by receipt status
- Unit tests over the clamping logic in the receive path

---

Self-directed portfolio project.
