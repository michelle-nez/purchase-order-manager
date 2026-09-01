# Configuration

Every setting this application reads, where it comes from, and which ones matter.

**One setting has to be supplied** — the connection string. Everything else has a
working default.

## Where settings come from

ASP.NET Core layers configuration sources, and later sources win:

1. `appsettings.json`
2. `appsettings.{Environment}.json` — here, `appsettings.Development.json`
3. **User Secrets — Development environment only**
4. Environment variables
5. Command-line arguments

### The precedence trap

**User Secrets override `appsettings.json`, and they are only loaded in Development.**

- Putting a connection string into `appsettings.json` while a User Secrets value
  exists appears to do nothing. The secret is winning. Change the secret, not the file.
- In **Production**, User Secrets are not loaded at all. A deployed instance must get
  its connection string from an environment variable or the host's own configuration.
  There is no production config file in this repository.

## appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PurchasingDatabase": ""
  }
}
```

| Setting | Meaning |
|---|---|
| `Logging:LogLevel:Default` | `Information` — this is what surfaces the save-failure log written by `PurchaseOrderEdit` |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` — quiets per-request framework noise |
| `AllowedHosts` | `*`, the framework default. Worth narrowing if ever deployed |
| `ConnectionStrings:PurchasingDatabase` | **Intentionally empty.** The key documents the shape; the value lives in User Secrets |

**The empty connection string is the whole security posture of this repo.** Do not
fill it in — a value here would be committed the moment it is saved.

## appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

This overrides the base file with **identical values**, so it currently changes
nothing. It is the stock template file, left in place.

It would earn its place with EF Core command logging:
`"Microsoft.EntityFrameworkCore.Database.Command": "Information"`. That is more useful
in this app than in the others, because the totals are computed in C# after loading
lines — watching the SQL makes it obvious how much is being pulled back per page.

## The connection string

| | |
|---|---|
| Key | `ConnectionStrings:PurchasingDatabase` |
| Read by | `Program.cs`, via `GetConnectionString("PurchasingDatabase")` |
| Development source | User Secrets |
| Development value | `Server=(localdb)\MSSQLLocalDB;Database=PurchaseOrderManager;Trusted_Connection=True;TrustServerCertificate=True` |

`Trusted_Connection=True` uses Windows authentication, so the development setup has
**no password anywhere**.

The provider is SQL Server on both sides — moving to a real server changes the
connection string and nothing else.

## User Secrets

`PurchaseOrders.Web.csproj` carries the id tying the project to its secrets file:

```xml
<UserSecretsId>9f2647e3-d6a8-4ec9-98e7-8efe08bd94c8</UserSecretsId>
```

The id is not a secret — it is only a folder name. The file lives outside the
repository at:

```
%APPDATA%\Microsoft\UserSecrets\9f2647e3-d6a8-4ec9-98e7-8efe08bd94c8\secrets.json
```

Per-machine and per-Windows-user, so it does not travel with a clone.

## Environments

`ASPNETCORE_ENVIRONMENT` is the only environment variable this app cares about. Both
launch profiles set it to `Development`.

| | Development | Production |
|---|---|---|
| Unhandled exceptions | Developer exception page, full stack trace | `/Error` page, no detail |
| HSTS | off | on |
| User Secrets | loaded | **not loaded** |

There is no `appsettings.Production.json` in the repository.

## Launch profiles

From `PurchaseOrders.Web/Properties/launchSettings.json`:

| Profile | URLs | Environment |
|---|---|---|
| `http` | http://localhost:5126 | Development |
| `https` | https://localhost:7145 and http://localhost:5126 | Development |

`launchSettings.json` is a **local development file only** — not read when the app
runs outside Visual Studio or `dotnet run`.

## Business rules are in code, not configuration

Worth stating, because these are the values someone would go looking for in a settings
file and would not find:

| Rule | Where it lives |
|---|---|
| Order total, quantities, receipt status | Derived properties on `PurchaseOrder` |
| Line total, outstanding quantity | Derived properties on `PurchaseOrderLine` |
| Received can never exceed ordered | `Math.Min` in `PurchaseOrderDetail.ReceiveAsync` |
| Valid quantity and cost ranges | `[Range]` attributes on the models |

**None of these is configurable, deliberately.** They are correctness rules rather
than preferences — a deployment that could switch off the receive clamp would be able
to corrupt its own data.

## Build-time settings

`PurchaseOrders.Web.csproj`:

| Property | Value | Effect |
|---|---|---|
| `TargetFramework` | `net10.0` | |
| `Nullable` | `enable` | Nullable reference type warnings on |
| `ImplicitUsings` | `enable` | Common namespaces without `using` |
| `BlazorDisableThrowNavigationException` | `true` | Stops `NavigateTo` throwing during a lifecycle method — which the unknown-id redirects rely on |

## What is not configured

- **No authentication or authorization**
- **No external services** — no API keys, no email, no storage
- **No Swagger/OpenAPI** — there is no HTTP API in this project
- **No feature flags, health checks, or CORS policy**
- **No custom logging providers** — console only, at the levels above

The complete list of things that must be supplied to run this app is: **the connection
string.**

## Recommendations

| Recommendation | Why |
|---|---|
| Narrow `AllowedHosts` if deployed | `*` accepts any Host header |
| Add `appsettings.Production.json` if deployed | No production config exists today |
| Add EF Core command logging to the Development file | Makes the load-every-line cost visible |
