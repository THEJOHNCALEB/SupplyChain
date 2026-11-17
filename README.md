# SupplyChain

A traceable agricultural marketplace for Nigerian produce. Farmers list a harvest, distributors and retailers acquire lots along the chain, and customers buy from retail sellers with a full custody trail behind every order.

The application is a single ASP.NET Core MVC app backed by SQLite. It is designed to run locally with zero external services.

## Features

- Role-based accounts for farmers, distributors, retailers, customers and admins.
- Marketplace search, category and stage filters, sorting and pagination.
- Lot acquisition flow that moves produce from Farm to Distribution to Retail.
- Cart, checkout, delivery pricing and order status tracking.
- Seller dashboards for revenue, inventory, sales and settlement history.
- Admin views for users and orders.
- Custody transfer records for every hand-off.
- Responsive UI, light theme by default and a dark mode toggle.
- Demo data seeded on first run.

## Requirements

- .NET 8 SDK
- SQLite (bundled with the EF Core provider, no separate install)

## Quick start

```bash
dotnet restore
dotnet run --project src/SupplyChain.Web/SupplyChain.Web.csproj
```

Open the URL printed in the console. The SQLite database is created and seeded automatically on first start.

To start on a fixed port:

```bash
dotnet run --project src/SupplyChain.Web/SupplyChain.Web.csproj --urls http://localhost:5080
```

## Demo accounts

All demo accounts use the password `Demo@123`.

| Role | Email |
| --- | --- |
| Admin | `admin@supplychain.dev` |
| Farmer | `amina@farm.dev` |
| Farmer | `musa@farm.dev` |
| Distributor | `tunde@distro.dev` |
| Retailer | `chioma@freshmart.dev` |
| Retailer | `emeka@market.dev` |
| Customer | `dayo@customer.dev` |
| Customer | `zainab@customer.dev` |

## How the chain works

1. A farmer lists a lot at the **Farm** stage.
2. A distributor acquires the lot, pays the farm price and relists it at the **Distribution** stage.
3. A retailer acquires the lot, pays the distribution price and relists it at the **Retail** stage.
4. A customer adds retail produce to a cart and checks out.
5. Every acquisition and sale is recorded as a custody transfer.

Selling prices cannot fall below the price paid by the seller. Delivery is a flat ₦2,500 and is free for orders above ₦100,000.

## Project structure

```text
src/
  SupplyChain.Domain/          Entities, enums, contracts, pricing rules
  SupplyChain.Infrastructure/  EF Core, SQLite, services, seeding
  SupplyChain.Web/             MVC controllers, Razor views, CSS and JS
tests/
  SupplyChain.Tests/           Test project
```

## Useful commands

```bash
dotnet build SupplyChain.sln
dotnet test SupplyChain.sln
dotnet format SupplyChain.sln
```

Runtime data and uploads are ignored by git. Delete `src/SupplyChain.Web/supplychain.db` to reset the app to its seeded demo state.

## License

MIT. See [LICENSE](LICENSE).
