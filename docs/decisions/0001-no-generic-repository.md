# 0001 — Services use `AppDbContext` directly, without a generic repository

**Status:** accepted

## Context

The original code had a note to "look into the generic repository pattern". A generic
`IRepository<T>` with `GetAll`, `GetById`, `Add`, `Update`, `Delete` was considered.

## Decision

Application services keep using `AppDbContext` directly.

- `DbContext` already is a unit of work and each `DbSet<T>` already is a repository.
- The queries here are shaped per screen (projections, paging, `Include`), which a generic
  interface either hides behind `IQueryable` (leaking EF Core anyway) or cannot express.
- Tests can run against the EF Core in-memory or SQLite provider instead of mocking a
  repository layer.

## Consequences

Services depend on EF Core. If a second storage technology is ever needed, a repository
per aggregate (not a generic one) is the place to start.
