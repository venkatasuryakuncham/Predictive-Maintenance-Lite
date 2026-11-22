# PredictiveMaintenance

## Overview

PredictiveMaintenance is an ASP.NET Core 8 Web API that monitors industrial assets (pumps, motors, etc.) by ingesting sensor readings, comparing them against configurable thresholds, and automatically raising maintenance tickets when readings exceed safe limits. The solution uses Entity Framework Core 8 with SQL Server LocalDB for persistence and exposes a Swagger UI for interactive exploration.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET 8 SDK | 8.0.x |
| SQL Server LocalDB | Ships with Visual Studio 2022 |
| Visual Studio 2022 | 17.x (Community or higher) |
| EF Core CLI tools | `dotnet tool install --global dotnet-ef` |

---

## First-Time Setup (run once per machine)

Open a terminal in the solution root (the folder containing `PredictiveMaintenance.sln`):

```bash
# 1. Restore all NuGet packages
dotnet restore

# 2. Create the initial EF Core migration
dotnet ef migrations add InitialCreate \
  --project PredictiveMaintenance.Data \
  --startup-project PredictiveMaintenance.API

# 3. Apply the migration and create the database
dotnet ef database update \
  --project PredictiveMaintenance.Data \
  --startup-project PredictiveMaintenance.API
```

After step 3, open SQL Server Object Explorer (or SSMS) and verify that **PredictiveMaintenanceDb** contains these 5 tables:
- `Assets`
- `Sensors`
- `Readings`
- `Thresholds`
- `Tickets`

---

## How to Run the API

```bash
dotnet run --project PredictiveMaintenance.API
```

Then navigate to `https://localhost:7001/swagger` — the Swagger UI lists all available endpoints.

Alternatively, press **F5** in Visual Studio 2022 to launch with the debugger attached; the browser opens automatically at `/swagger`.

---

## Team Rules

> **Do NOT modify `PredictiveMaintenance.Models` or `PredictiveMaintenance.Data`.**
> All domain models and the EF Core context are locked after Day 1/2. Changes there break everyone's migrations.

| Person | Responsibility |
|--------|---------------|
| **P1** | Models, Data, API scaffold, QueryService (this repo) |
| **P2** | Add `AssetsController`, `SensorsController`, `ReadingsController` in `PredictiveMaintenance.API/Controllers/` |
| **P3** | Add `ThresholdsController`, `TicketsController` in `PredictiveMaintenance.API/Controllers/` and `MaintenanceService` in `PredictiveMaintenance.API/Services/` |
| **P4** | Add a new `PredictiveMaintenance.Simulator` console project to the solution for generating synthetic sensor readings |

---

## Connection String

Default connection targets SQL Server LocalDB and is defined in `appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=PredictiveMaintenanceDb;Trusted_Connection=True;TrustServerCertificate=True
```

Override it in `appsettings.Development.json` or with a `ConnectionStrings__DefaultConnection` environment variable if you use a full SQL Server instance instead.
#
