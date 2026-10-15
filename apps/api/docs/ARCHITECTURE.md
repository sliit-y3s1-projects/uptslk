# API Architecture

The API is one ASP.NET Core 8 project with a layered folder structure. Every request goes through the same layers, so you always know where code belongs.

```
HTTP request
   │
Controller ──► IXService ──► IXRepository ──► AppDbContext ──► PostgreSQL
                  │
                  ├──► IUnitOfWork.SaveChangesAsync()
                  └──► throws an AppException on failure
                              │
                  GlobalExceptionHandler ──► ProblemDetails JSON
```

## Folders

| Folder | What goes in it |
| --- | --- |
| `Controllers/` | HTTP endpoints. Routing, `[Authorize]`, model binding, and one call to a service. |
| `DTO/<Feature>/` | Request and response types for one feature: `XRequests.cs`, `XResponses.cs`, `XFilter.cs`. |
| `Interfaces/Services/` | Service interfaces (`IXService`) and interfaces for outside systems (`IPaymentGateway`, `IImageStorageService`). |
| `Interfaces/Repositories/` | Repository interfaces, `IRepository<T>` and `IUnitOfWork`. |
| `Services/` | Business logic. One `XService` per feature. |
| `Services/External/` | Clients for outside systems: Stripe, Supabase storage, Gemini. |
| `Services/AgentRecovery/` | The AI incident recovery engine (agents, tools, planner). Kept as one module. |
| `Repositories/` | EF Core queries. One `XRepository` per entity, built on `Repository<T>`. |
| `Entities/` | EF Core entity classes. `Entities/Enums/` holds their enums. |
| `Data/` | `AppDbContext`, `UnitOfWork`, entity configuration, converters, seeding and the health check. |
| `Data/Configurations/` | One `IEntityTypeConfiguration<T>` per entity: keys, indexes, lengths and relationships. |
| `Exceptions/` | `AppException` and one exception type per HTTP error status. |
| `Common/Constants/` | Role names, claim names, cookie names and rate limit policy names. |
| `Common/Extensions/` | Startup setup (`AddApiServices`, `AddDatabase`, …) and small extension methods. |
| `Common/Middleware/` | `GlobalExceptionHandler`. |
| `Common/Filters/` | MVC filters, such as sending queued notifications after a successful request. |
| `Common/Helpers/` | Small shared helpers: Sri Lanka time, timetable times, text cleanup. |
| `Hubs/` | SignalR hubs. |
| `Migrations/` | EF Core migrations. They stay at the project root so their namespace never changes. |

## Layer rules

| Layer | Does | Never does |
| --- | --- | --- |
| Controller | Routes the request, checks the role with `[Authorize]`, calls one service method, returns `Ok`, `Created` or `NoContent`. | Use `AppDbContext` or a repository, contain `if` business rules, build error objects. |
| Service | Business rules, centre access checks with `ICurrentUserService`, maps entities to response DTOs, decides when to save. | Write EF queries (`Include`, `Where`). Return entities to the controller. |
| Repository | EF queries, includes, filters and row locks. Returns entities. | Return DTOs, contain business rules, call `SaveChanges`. |
| Unit of work | `SaveChangesAsync`, `BeginTransactionAsync`, `DiscardChanges`. | Anything else. |

All repositories share one `AppDbContext` per request. That is why only `IUnitOfWork` saves: one save writes every change made through any repository.

## Errors

Services never return error codes. They throw:

| Exception | Status |
| --- | --- |
| `BadRequestException` | 400 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `BadGatewayException` | 502 |
| `ServiceUnavailableException` | 503 |

`GlobalExceptionHandler` turns them into ProblemDetails JSON. The message is sent as both `detail` and `error`, because the web and mobile apps read `error` first:

```json
{ "status": 404, "title": "Not Found", "detail": "Trip not found.", "error": "Trip not found." }
```

- When one request breaks several rules, pass the list as well: `new BadRequestException(message, errors)`. It is sent as `errors`.
- For extra fields, use `Details`: `new ConflictException(message) { Details = { ["conflictingTripIds"] = ids } }`.
- Any other exception becomes a 500 with a generic message, and the real error is logged.
- Identity results use `result.ThrowIfFailed()`.

## Naming

- Services: `TripService : ITripService`. Repositories: `TripRepository : ITripRepository`.
- DTOs: `CreateXRequest`, `UpdateXRequest`, `XFilter` for query strings, `XResponse` for one item, `XListItemResponse` for lists, `XCreatedResponse` for the body of a 201.
- Each response record has a `static From(entity)` method that does the mapping.
- Every async method takes a `CancellationToken`.
- Use the constants in `Common/Constants` instead of strings, for example `[Authorize(Roles = AppRoles.Managers)]`.
- Comments are short, single-line and in plain English. Explain why, not what.

## Adding a feature

1. Add the entity to `Entities/`, its configuration to `Data/Configurations/`, and a `DbSet` to `AppDbContext`.
2. Discuss the schema change with the team, then run `dotnet ef migrations add <DescriptiveName>` from `apps/api`.
3. Add request, response and filter types to `DTO/<Feature>/`.
4. Add `IXRepository` in `Interfaces/Repositories/` and `XRepository : Repository<X>` in `Repositories/`. Only add query methods the service needs, named by intent (`GetActiveForRouteAsync`, not `Query`).
5. Add `IXService` in `Interfaces/Services/` and `XService` in `Services/`. Throw exceptions for every failure.
6. Register both in `Common/Extensions/ApplicationServiceExtensions.cs`, in alphabetical order.
7. Add a controller where every action is one service call.
8. Run `dotnet build` and the tests in `apps/api.Tests`.

## Known exceptions to the rules

These older services still use `AppDbContext` directly. The tests build them with a `DbContext`, so moving them to repositories also means updating the tests.

- `TripConflictService` and `VehicleMaintenanceRules` (trip and maintenance clash checks)
- `BookingCancellationService` and `BookingPaymentService` (cancellations, refunds and Stripe payments)
- `MobileNotificationService` (in-app notifications)
- The tools and workflow in `Services/AgentRecovery/`

New code should not copy this pattern.
