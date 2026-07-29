phase# Children's Toy Store — Dockerized Implementation Steps (Mac + VS Code)

Stack: **ASP.NET Core Web API (.NET 8)** + **EF Core with SQLite** + **React (Vite)**, everything running in Docker containers via `docker-compose`. No local .NET or Node install required — only Docker and VS Code.

---

## Phase 0 — Prerequisites (Mac)

- [ ] Install [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop/) (Apple Silicon or Intel build, matching your Mac)
- [ ] Install [VS Code](https://code.visualstudio.com/)
- [ ] In VS Code, install these extensions:
  - **Dev Containers** (`ms-vscode-remote.remote-containers`) — lets VS Code attach to a running container
  - **Docker** (`ms-azuretools.vscode-docker`) — manage images/containers from the sidebar
  - **C# Dev Kit** (optional, for API IntelliSense while editing from inside a container)
  - **ES7+ React snippets** (optional, for the frontend)

Verify Docker works:
```bash
docker --version
docker compose version
```

- [ ] Confirm Docker Desktop is running (whale icon in the menu bar) before continuing

---

## Phase 1 — Project folder structure

Create the overall layout first — two subfolders (API, frontend) plus a root `docker-compose.yml` that orchestrates both.

```bash
mkdir ToyStore && cd ToyStore
mkdir data   # this is where the SQLite .db file will live on your Mac, mounted into the API container
```

Target structure once everything is scaffolded:
```
ToyStore/
  docker-compose.yml
  data/                     # SQLite db file lives here (persisted on host)
  ToyStore.Api/
    Dockerfile
    .dockerignore
    ... (API source)
  toystore-web/
    Dockerfile
    Dockerfile.prod          # optional, for a production build later
    .dockerignore
    ... (React source)
```

---

## Phase 2 — Scaffold the API (inside a container, no local .NET needed)

You don't need the .NET SDK installed on your Mac — use a throwaway container to run `dotnet new`:

```bash
docker run --rm -v "$(pwd)/ToyStore.Api:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet new webapi -n ToyStore.Api --use-controllers -o .
```

This drops a full ASP.NET Core Web API project into `ToyStore.Api/` using the official .NET 8 SDK image, without installing anything locally.

Add the NuGet packages the same way:
```bash
docker run --rm -v "$(pwd)/ToyStore.Api:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite

docker run --rm -v "$(pwd)/ToyStore.Api:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet add package Microsoft.EntityFrameworkCore.Design

docker run --rm -v "$(pwd)/ToyStore.Api:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

docker run --rm -v "$(pwd)/ToyStore.Api:/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet add package BCrypt.Net-Next
```

**Tip:** this pattern (`docker run --rm -v "$(pwd)/X:/app" -w /app <sdk-image> <command>`) is your substitute for having `dotnet` installed locally. You'll use it again for EF migrations in Phase 5.

---

## Phase 3 — Write the API Dockerfile (development mode, with hot reload)

**`ToyStore.Api/Dockerfile`**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
EXPOSE 8080

# Restore separately so Docker caches this layer unless csproj changes
COPY *.csproj ./
RUN dotnet restore

COPY . .

# dotnet watch gives you hot reload while editing in VS Code on the host
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]
```

**`ToyStore.Api/.dockerignore`**
```
bin/
obj/
*.db
```

This is a **dev-mode** Dockerfile (uses the full SDK image, runs `dotnet watch`). It's intentionally not optimized for size — that comes later if you deploy (Phase 13 covers a slim production build).

---

## Phase 4 — Write the React Dockerfile (development mode, with hot reload)

Scaffold the frontend the same containerized way, using the Node image:
```bash
cd ..
docker run --rm -v "$(pwd)/toystore-web:/app" -w /app node:20 \
  npx create-vite@latest . -- --template react
```

**`toystore-web/Dockerfile`**
```dockerfile
FROM node:20

WORKDIR /app
COPY package*.json ./
RUN npm install

COPY . .
EXPOSE 5173

# --host is required so Vite's dev server is reachable from outside the container
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
```

**`toystore-web/.dockerignore`**
```
node_modules/
dist/
```

Add the extra packages you'll need (again, via a throwaway container so you don't need Node locally):
```bash
docker run --rm -v "$(pwd)/toystore-web:/app" -w /app node:20 \
  npm install react-router-dom axios
```

---

## Phase 5 — docker-compose.yml (ties both services together)

**`ToyStore/docker-compose.yml`**
```yaml
services:
  api:
    build: ./ToyStore.Api
    ports:
      - "8080:8080"
    volumes:
      - ./ToyStore.Api:/app          # live-mount source so edits in VS Code trigger dotnet watch
      - /app/bin                      # anonymous volumes: keep container's own build output
      - /app/obj                      # separate from your Mac's filesystem (avoids OS mismatch issues)
      - ./data:/app/data              # SQLite file persists here on your Mac
    environment:
      - ConnectionStrings__Default=Data Source=/app/data/toystore.db
      - ASPNETCORE_ENVIRONMENT=Development

  web:
    build: ./toystore-web
    ports:
      - "5173:5173"
    volumes:
      - ./toystore-web:/app
      - /app/node_modules             # anonymous volume: keeps container's own node_modules
    environment:
      - VITE_API_URL=http://localhost:8080/api
    depends_on:
      - api
```

Key points worth understanding:
- The **anonymous volumes** (`/app/bin`, `/app/obj`, `/app/node_modules`) prevent your Mac's host filesystem from overwriting the container's compiled output/dependencies with host-incompatible files — this is the single most common Docker-on-Mac gotcha.
- The **named bind mounts** (`./ToyStore.Api:/app`, `./toystore-web:/app`) are what give you hot reload: edit a file in VS Code on your Mac, the change is instantly visible inside the container, and `dotnet watch` / Vite's dev server picks it up.
- `./data:/app/data` means the SQLite file lives in `ToyStore/data/toystore.db` on your actual Mac, so it survives `docker compose down` and container rebuilds.

---

## Phase 6 — First run

```bash
cd ToyStore
docker compose up --build
```

- [ ] Confirm the API container logs show `dotnet watch` running and listening on port 8080
- [ ] Confirm the web container logs show Vite ready on port 5173
- [ ] Visit `http://localhost:8080/swagger` in your browser to confirm the API is reachable
- [ ] Visit `http://localhost:5173` to confirm the default Vite page loads

Stop everything with `Ctrl+C`, or in a second terminal: `docker compose down`.

---

## Phase 7 — Wire up the DbContext for SQLite

Same code as a non-Docker setup — the only difference is the connection string comes from an environment variable instead of `appsettings.json`.

**`ToyStore.Api/Data/ToyStoreDbContext.cs`**
```csharp
using Microsoft.EntityFrameworkCore;

public class ToyStoreDbContext : DbContext
{
    public ToyStoreDbContext(DbContextOptions<ToyStoreDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}
```

**In `Program.cs`:**
```csharp
builder.Services.AddDbContext<ToyStoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
```

Because `docker-compose.yml` sets `ConnectionStrings__Default` as an environment variable, ASP.NET Core's configuration system automatically maps it to `builder.Configuration.GetConnectionString("Default")` — the double underscore `__` is how env vars represent the `:` nesting from `appsettings.json`. You don't need a matching entry in `appsettings.json` for this to work, though keeping a fallback there is fine for clarity.

Add your `Models/` classes exactly as planned before (Product, Category, User, CartItem, Order, OrderItem) — this part is identical whether Dockerized or not.

---

## Phase 8 — EF Core migrations, run inside the container

With everything already running via `docker compose up`, open a second terminal and exec into the running API container to run EF Core commands:

```bash
docker compose exec api dotnet tool install --global dotnet-ef
docker compose exec api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations add InitialCreate'
docker compose exec api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update'
```

- [ ] Confirm a `Migrations/` folder appears in `ToyStore.Api/` on your Mac (it's bind-mounted, so container output shows up on the host)
- [ ] Confirm `ToyStore/data/toystore.db` now exists on your Mac
- [ ] Open it with DB Browser for SQLite (installed natively on your Mac — this one tool doesn't need to be Dockerized) to confirm tables were created

**Tip:** since you'll run `dotnet ef` commands repeatedly, consider adding this line near the top of the API `Dockerfile` so the tool is pre-installed in the image instead of reinstalling it each session:
```dockerfile
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
```
Then just run `docker compose exec api dotnet ef migrations add InitialCreate` directly.

---

## Phase 9 — Build out the API (identical to non-Docker plan)

From here, the actual C# you write is unchanged — Docker only affects *how you run things*, not the API design. Follow this order, editing files in VS Code on your Mac (changes hot-reload inside the container):

1. **Catalog endpoints** — `GET /api/products`, `GET /api/products/{id}`, `GET /api/categories`
2. **JWT auth** — `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
3. **Admin-protected catalog writes** — `[Authorize(Roles = "Admin")]` on product/category create/update/delete
4. **Cart** — `GET/POST/PUT/DELETE /api/cart/items` (all `[Authorize]`, scoped to the JWT's user ID)
5. **Orders** — `POST /api/orders` (cart → order, clears cart), `GET /api/orders`, `GET /api/orders/{id}`, `PUT /api/orders/{id}/status` (admin)

For the JWT signing key and CORS origin, add these to `docker-compose.yml` under the `api` service's `environment:` block rather than hardcoding them:
```yaml
    environment:
      - ConnectionStrings__Default=Data Source=/app/data/toystore.db
      - ASPNETCORE_ENVIRONMENT=Development
      - Jwt__Key=replace-with-a-long-random-secret-string
      - Jwt__Issuer=ToyStoreApi
      - Jwt__ExpiresMinutes=60
```

For CORS in `Program.cs`, allow the frontend's container-network address as well as localhost:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

- [ ] Test each endpoint via `http://localhost:8080/swagger` as you build it

---

## Phase 10 — Build out the React frontend (identical structure to non-Docker plan)

```
toystore-web/src/
  api/
    client.js
    products.js
    auth.js
    cart.js
    orders.js
  context/
    AuthContext.jsx
    CartContext.jsx
  pages/
    HomePage.jsx
    ProductListPage.jsx
    ProductDetailPage.jsx
    CartPage.jsx
    OrderHistoryPage.jsx
    LoginPage.jsx
    RegisterPage.jsx
    admin/
      AdminProductsPage.jsx
      AdminOrdersPage.jsx
  components/
    ProductCard.jsx
    Navbar.jsx
    ProtectedRoute.jsx
```

**`src/api/client.js`** — use the env var from `docker-compose.yml` instead of a hardcoded URL:
```javascript
import axios from 'axios';

const client = axios.create({ baseURL: import.meta.env.VITE_API_URL });

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default client;
```

Build order (same as the non-Docker plan):
1. Product catalog (list + detail)
2. Auth (register/login, `AuthContext`, JWT in `localStorage`)
3. Cart
4. Checkout → order confirmation (no payment form)
5. Order history
6. Admin panel, gated by role check

- [ ] After each feature, refresh `http://localhost:5173` — Vite's dev server hot-reloads automatically

---

## Phase 11 — Everyday workflow

```bash
# Start everything
docker compose up

# Start in the background (detached)
docker compose up -d

# View logs for one service
docker compose logs -f api
docker compose logs -f web

# Rebuild after changing a Dockerfile or adding a package
docker compose up --build

# Stop everything
docker compose down

# Stop and also wipe the SQLite volume (careful — deletes your data)
docker compose down -v
```

Since your SQLite file lives in `./data` on the Mac (not in an anonymous volume), running `docker compose down` alone will **not** delete your data — only `-v` combined with removing that bind mount would, and since it's a bind mount to a real folder, your data survives even `-v`. To fully reset the database, delete `ToyStore/data/toystore.db` manually and re-run the migrations.

---

## Phase 12 — Editing experience in VS Code

You have two reasonable options:

**Option A — edit from the host, run in containers (simplest, recommended to start).**
Just open the `ToyStore/` folder normally in VS Code. Install the C# Dev Kit for IntelliSense — it works fine against the source files even though execution happens in Docker, since the files physically exist on your Mac (bind-mounted). This is what the steps above assume.

**Option B — attach VS Code directly into the running container (Dev Containers extension).**
Gives you IntelliSense/debugging that exactly matches the container's installed SDK version, useful if you hit host/container tooling mismatches. In VS Code: Command Palette → "Dev Containers: Attach to Running Container" → select the `api` container. This is optional — most people are fine with Option A for a project this size.

---

## Phase 13 — Production build (optional, later)

When you're ready to deploy rather than just develop locally, add slim multi-stage Dockerfiles instead of the dev ones above.

**`ToyStore.Api/Dockerfile.prod`**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ToyStore.Api.dll"]
```

**`toystore-web/Dockerfile.prod`**
```dockerfile
FROM node:20 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
```

- [ ] Deploy targets to consider: Render or Railway (both support `docker-compose`-style multi-service deploys easily), or Azure App Service for Containers
- [ ] Swap SQLite for a managed Postgres/SQL Server if you outgrow a single-file database in production — this is a one-line EF Core provider change plus a new migration

---

## Phase 14 — Migrate to SQL Server (optional, do this once the SQLite version works end-to-end)

This is a deliberate follow-up exercise, not a day-one requirement. The point of doing it *after* the app already works is that you'll clearly see how much EF Core insulates your code from the underlying database — your models, DbContext shape, LINQ queries, and controllers stay untouched. Only the provider package, one line in `Program.cs`, the connection string, and `docker-compose.yml` change.

### Step 1 — Swap the NuGet package

```bash
docker compose exec api dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
docker compose exec api dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Step 2 — Change the provider in `Program.cs`

```csharp
// Before
options.UseSqlite(builder.Configuration.GetConnectionString("Default"));

// After
options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
```

### Step 3 — Add the `db` service and update `docker-compose.yml`

```yaml
services:
  api:
    build: ./ToyStore.Api
    ports:
      - "8080:8080"
    volumes:
      - ./ToyStore.Api:/app
      - /app/bin
      - /app/obj
    environment:
      - ConnectionStrings__Default=Server=db;Database=ToyStore;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
      - ASPNETCORE_ENVIRONMENT=Development
      - Jwt__Key=replace-with-a-long-random-secret-string
      - Jwt__Issuer=ToyStoreApi
      - Jwt__ExpiresMinutes=60
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test: ["CMD", "/opt/mssql-tools18/bin/sqlcmd", "-C", "-S", "localhost", "-U", "sa", "-P", "YourStrong!Passw0rd", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  web:
    build: ./toystore-web
    ports:
      - "5173:5173"
    volumes:
      - ./toystore-web:/app
      - /app/node_modules
    environment:
      - VITE_API_URL=http://localhost:8080/api
    depends_on:
      - api

volumes:
  sqlserver-data:
```

**Why the `healthcheck` matters:** SQL Server takes several seconds to finish starting up inside its container — unlike SQLite, which has no startup delay at all since there's no separate server process. Without `depends_on: condition: service_healthy`, the API container can start and try to connect before SQL Server is actually ready to accept connections, causing confusing early-connection failures. The healthcheck makes Docker wait until `sqlcmd` can successfully query the server before starting the `api` service.

### Step 4 — Delete the old SQLite migrations and regenerate them

EF Core migrations are provider-specific (they contain SQL Server or SQLite-flavored SQL under the hood), so you can't reuse SQLite migrations against SQL Server — you need a fresh set.

```bash
# Remove the old migrations folder (SQLite-specific)
rm -rf ToyStore.Api/Migrations

# Rebuild containers so the new SqlServer package is picked up
docker compose up --build -d

# Generate fresh migrations against SQL Server
docker compose exec api dotnet ef migrations add InitialCreate
docker compose exec api dotnet ef database update
```

- [ ] Confirm the API starts without connection errors (check `docker compose logs -f api`)
- [ ] Confirm tables exist by connecting with Azure Data Studio, the VS Code `mssql` extension, or `sqlcmd` at `localhost:1433` (user `sa`, the password from `MSSQL_SA_PASSWORD`)
- [ ] Re-run your Phase 6–9 endpoint tests (Swagger) to confirm catalog, auth, cart, and orders all still work exactly as before

### Step 5 — Notice what didn't change

Worth pausing on explicitly, since this is the actual learning payoff: your `Models/`, `ToyStoreDbContext.cs`, every controller, and every LINQ query are byte-for-byte identical to the SQLite version. The only files you touched were the `.csproj` (via package swap), one line in `Program.cs`, `docker-compose.yml`, and the migrations folder. That boundary — provider-specific vs. provider-agnostic — is the concrete thing EF Core is buying you as an ORM.

### Reverting back to SQLite

If you want to go back (e.g. to compare side by side), keep a copy of your SQLite `Migrations/` folder and `docker-compose.yml` before starting this phase — swapping providers back and forth means matching the migrations folder to whichever provider is active, they aren't interchangeable.

---

## Quick reference

```bash
# One-time setup
docker compose build

# Day-to-day
docker compose up          # start API + frontend, logs in terminal
docker compose exec api dotnet ef migrations add <Name>   # new migration
docker compose exec api dotnet ef database update         # apply migrations
docker compose down        # stop everything
```

Frontend: `http://localhost:5173`
API + Swagger: `http://localhost:8080/swagger`
SQLite file (inspect with DB Browser for SQLite, installed natively on your Mac): `ToyStore/data/toystore.db`
