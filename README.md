# WorkOrderHub

A maintenance work-order API built with **C#, ASP.NET Core 8, Entity Framework Core and SQLite**.
Teams log equipment problems, a dispatcher assigns them to technicians by priority and due date,
and a reporting endpoint summarizes the backlog with plain SQL.

## Features

- **REST API** for work orders and technicians, with request validation and RFC 7807 problem responses.
- **Status lifecycle** (Open → Assigned → InProgress → Resolved → Closed, plus Cancelled) enforced in the
  domain model. Invalid moves return `409 Conflict`.
- **Dispatcher** that uses two heaps (`PriorityQueue`): work orders come out highest priority first, then
  earliest due date, and each goes to the technician with the fewest active orders who still has capacity.
- **SQL reports**: counts by status, open and overdue backlog by priority (a CTE with `GROUP BY` and `CASE`),
  and average hours to resolve.
- **Database design**: EF Core model with indexes on `(Status, Priority)` and `DueAtUtc`, a foreign key from
  work orders to technicians, and a check constraint on technician capacity.
- **Tests**: 30 xUnit tests. Unit tests cover the lifecycle table and dispatcher; integration tests run the
  real API in memory (`WebApplicationFactory`) against an in-memory SQLite database with a fake clock.
- **CI**: GitHub Actions builds and tests every push and pull request.

## Project layout

```
src/WorkOrderHub.Api
  Controllers/   HTTP endpoints (work orders, technicians, dispatch, reports)
  Domain/        WorkOrder, Technician, lifecycle rules, domain exceptions
  Services/      business logic, dispatcher, SQL reports, clock abstraction
  Data/          EF Core DbContext and table configuration
  Middleware/    error handling and logging
tests/WorkOrderHub.Tests
  LifecycleTests, DispatcherTests (unit) and ApiTests (integration)
```

## Run it

```bash
dotnet run --project src/WorkOrderHub.Api
# Swagger UI: http://localhost:5xxx/swagger (port printed on startup)
```

## Test it

```bash
dotnet test
```

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/work-orders?status=&priority=&page=&pageSize=` | List with filters and paging |
| GET | `/api/work-orders/{id}` | Get one work order |
| POST | `/api/work-orders` | Create (title, assetTag, description, priority, slaHours) |
| PATCH | `/api/work-orders/{id}/status` | Move through the lifecycle |
| GET | `/api/technicians` | List technicians with current load |
| POST | `/api/technicians` | Add a technician |
| POST | `/api/dispatch/run` | Assign open work orders |
| GET | `/api/reports/summary` | Backlog and resolution report |
| GET | `/health` | Health check |
