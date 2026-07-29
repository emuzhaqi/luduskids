# LudusKids

A children's toy store web app: product catalog, accounts, cart, checkout, order
history, and an admin panel for managing products and orders.

**Stack:** ASP.NET Core 8 Web API + EF Core (SQLite) · React (Vite) · Docker Compose.
Everything runs in containers — no local .NET or Node install required, only Docker
and (optionally) VS Code.

## Project structure

```
luduskids/
  docker-compose.yml
  data/                     # SQLite db file, persisted on the host
  LudusKids.Api/            # ASP.NET Core Web API
  LudusKids.Api.Tests/      # xUnit integration + unit tests
  luduskids-web/            # React (Vite) frontend
```

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [VS Code](https://code.visualstudio.com/) (optional, for editing)

## Getting started

```bash
docker compose up --build
```

- API + Swagger: http://localhost:8080/swagger
- Frontend: http://localhost:5173

Both services hot-reload on file changes (`dotnet watch` for the API, Vite's dev
server for the frontend) — edit files on the host in VS Code and the containers
pick up the changes automatically.

Stop everything with `Ctrl+C`, or `docker compose down` from another terminal.

## Database & migrations

The SQLite file lives at `data/luduskids.db` on the host (bind-mounted into the
API container), so it survives `docker compose down` and rebuilds.

```bash
# Apply migrations (already applied for the current schema)
docker compose exec api dotnet ef database update

# Add a new migration after changing a model
docker compose exec api dotnet ef migrations add <Name>
```

To fully reset the database, delete `data/luduskids.db` and re-run
`dotnet ef database update`.

## API overview

All routes are under `/api`. JWT bearer auth; roles are `Customer` and `Admin`.

| Area | Routes |
|---|---|
| Auth | `POST /auth/register`, `POST /auth/login`, `GET /auth/me` |
| Catalog | `GET /products`, `GET /products/{id}`, `GET /categories` (public read; create/update/delete require `Admin`) |
| Cart | `GET/POST/PUT/DELETE /cart/items` (per-user, requires auth) |
| Orders | `POST /orders` (checkout, clears cart), `GET /orders`, `GET /orders/{id}`, `PUT /orders/{id}/status` (`Admin` only) |

Admins see every order via `GET /orders`; customers only see their own.

There's no seed/bootstrap script yet — the first admin has to be promoted
manually by setting `Role = 'Admin'` for a user row in `data/luduskids.db`.

## Running tests

```bash
docker run --rm -v "$(pwd):/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test LudusKids.Api.Tests
```

`LudusKids.Api.Tests` uses `WebApplicationFactory` against an in-memory SQLite
connection, so it boots the real app (DI, middleware, EF Core) with no
dependency on Docker or the on-disk database.

## Everyday workflow

```bash
docker compose up -d           # start in the background
docker compose logs -f api     # tail one service's logs
docker compose up --build      # rebuild after changing a Dockerfile or package
docker compose down            # stop everything (data/ is untouched)
```

## Notes

- NuGet packages are pinned to `8.0.x` — unpinned `dotnet add package` picks up
  whatever's newest on NuGet (currently EF Core 10.x), which isn't compatible
  with this project's `net8.0` target.
- The JWT signing key currently lives in `docker-compose.yml` as a plain
  environment variable. Fine for local dev; move it to a git-ignored `.env`
  file (or a real secrets manager) before this goes anywhere near production.
