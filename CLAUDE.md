# CLAUDE.md

This file gives Claude (or any AI assistant working on this repo) persistent context about the project. Keep it up to date as decisions change — it should always reflect *current* reality, not history (history goes in `plan.md` or commit messages).

---

## Project Summary

A farm management platform, starting with irrigation calculations and growing into full farm operations: assets/equipment, crop records, tasks, finance, reporting.

- **Users:** for now, multiple **managers** with full access to everything — owner/worker as distinct roles exist in the schema and UI but aren't used yet. Worker-specific restricted views and access are deferred to a later phase (see plan.md)
- **Deployment today:** local PC on the farm, served over LAN
- **Deployment path:** Docker Compose next → cloud later (same containers, different environment config)
- **Architecture style:** modular monolith — one deployable app, strict internal boundaries between domain modules, shared core platform

Full rationale and diagrams live in `docs/design.md` (the original high-level design doc). This file is the quick-reference for day-to-day/AI-assisted work; that one is the "why."

---

## Architecture Rules (don't violate these)

1. **Modules don't reach into each other's data directly.** A module talks to another module only through its public service/API layer, never by importing another module's internal repository/DB access code directly.
2. **All spatial references go through Core's Farm → Field/Block → Zone registry.** Don't let a module (e.g. Irrigation) invent its own notion of "location" — it references Core's Zone entity.
3. **Each module owns its own DB schema namespace** (e.g. `irrigation.*`, `assets.*`) and its own migrations. Don't create cross-schema foreign keys where avoidable — prefer referencing by ID and enforcing at the application layer.
4. **Calculation logic is a pure library, no I/O.** E.g. the irrigation ET₀/water-demand calculations must not depend on the DB or HTTP layer, so they stay independently testable.
5. **New feature = new module**, added via the module registry, not by bolting routes onto an existing module.
6. **No shortcuts that block the cloud migration path.** E.g. don't hardcode local filesystem paths outside the storage abstraction, don't assume LAN-only auth flows.

---

## Tech Stack (current)

| Layer | Choice |
|---|---|
| Backend | Python, FastAPI |
| Frontend | Blazor WebAssembly (C#), modules as Razor Class Libraries, lazy-loaded per route |
| Database | PostgreSQL, one schema per module |
| ORM/migrations | SQLAlchemy (2.x, ORM + Core) + Alembic for migrations |
| Auth | JWT sessions (`pyjwt` + `passlib`/`bcrypt`), roles stored in DB (`core.users.role`: owner/manager/worker); 12h token lifetime, no refresh tokens |
| Background jobs | APScheduler in-process (upgrade to Celery + Redis only if justified) |
| Packaging | Docker Compose: `app`, `db` |
| Reverse proxy | Caddy |
| Localization | `Microsoft.Extensions.Localization` (resx-based `IStringLocalizer<T>`), English default + Afrikaans (`af`); client-side only for now via `ILanguageService`/browser `localStorage` — seam left for per-user backend persistence once Core: Users + Auth ships |

> Prisma's Python client was considered but is unmaintained (deprecated March 2025, following Prisma's core rewrite from Rust to TypeScript) — not used for that reason.

> Localization currently covers only `Farm.Web.Core` chrome (nav, shell, login, dashboard) — `Farm.Web.Irrigation`'s `RunCalculator.razor` (~300 hardcoded strings across 27 calculators) is explicitly deferred to a future pass and has zero localization touchpoints today. Requires `<SatelliteResourceLanguages>en;af</SatelliteResourceLanguages>` and `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` in `apps/web/Farm.Web.Host.csproj` — the latter is required for Blazor WASM to allow switching `CurrentCulture`/`CurrentUICulture` away from the build-time default at runtime (confirmed by testing; omitting it throws "Blazor detected a change in the application's culture..."). Calculator numeric formatting is unaffected regardless of UI language since `RunCalculator.razor` always parses/formats with an explicit `CultureInfo.InvariantCulture`.

> Frontend is C#/Blazor WASM rather than React so the whole stack stays statically typed and each feature module can be a real compiled unit (Razor Class Library) rather than just a folder convention. Trade-off accepted: true drop-in-without-rebuild plugin loading is harder in .NET's assembly model than in JS — modules are lazy-loaded at runtime but still need to be referenced by the host project at build time.

> Auth: `passlib` (unmaintained since 2020) breaks with `bcrypt` ≥4.1 (probes a removed `__about__` attribute) — pin `bcrypt==4.0.1` explicitly alongside `passlib` rather than `passlib[bcrypt]`'s unpinned extra. The only way to create a user today is the CLI seed script (`python -m app.core.seed_admin`, in `apps/api/`) — no signup/registration endpoint exists; account creation UI is a future in-app admin screen. JWT session lifetime is 12 hours with no refresh token (`config.py`'s `jwt_expiry_minutes=720`) — an accepted v1 simplification for the LAN/low-risk deployment model. `require_role(*roles)` in `app/core/auth.py` is the reusable FastAPI dependency future modules should import for role-gating their own routers.

> **Role usage today vs. schema:** the schema/JWT/UI support three roles (owner/manager/worker), and that scaffolding stays as-is in the code. In practice, for now and until a later phase, only **manager** is used — multiple manager accounts, each with full access to everything (equivalent to what "owner" would mean). Worker accounts, the restricted Worker dashboard view, and any real owner-vs-manager distinction are deferred (see plan.md Parking Lot) — no code changes were made for this scoping decision. `Dashboard.razor`'s existing role-driven logic (owner/manager → full view + toggle, worker → locked-down view) already matches this model unmodified. The CLI seed script still hardcodes `Role.OWNER` for the account it creates, which behaves identically to a manager under the current logic — revisit whether to reseed as `Role.MANAGER` (or drop the distinction) once the manager-only account-creation UI is built.

> Update this table immediately if a tech choice changes — this is the source of truth, not the design doc.

---

## Repo Structure (target)

```
/apps
  /web                       → Blazor WASM host app
    Program.cs               → app entrypoint, route table, module assembly registry
    /Shell                    → layout, nav, auth UI, shared shell components
    App.razor                 → root router, configured for lazy assembly loading
  /web-modules
    /Farm.Web.Core            → Razor Class Library: core UI (auth, farm/field/zone registry screens) — referenced eagerly by host
    /Farm.Web.Irrigation      → Razor Class Library: irrigation UI — lazy-loaded on route match
    /Farm.Web.Assets          → Razor Class Library: assets UI (Phase 2+) — lazy-loaded
    /Farm.Web.Crops           → Razor Class Library: crops UI (Phase 3+) — lazy-loaded
    ...
  /api                       → FastAPI backend
    /app
      main.py                → app entrypoint, module/router registration
      /core                  → auth, users, roles, farm/field/zone registry, notifications, audit log, file storage
        models.py             → SQLAlchemy models (schema: core)
        router.py             → FastAPI router
        service.py            → business logic
        schemas.py            → Pydantic request/response models
      /irrigation             → irrigation module (same internal shape as /core)
        engine.py              → pure calculation library, no DB/HTTP imports
        models.py
        router.py
        service.py
        schemas.py
      /assets                 → assets module (Phase 2+)
      /crops                  → crops module (Phase 3+)
      ...
    /alembic
      versions/               → migration scripts, one history per module where practical (see Conventions)
      env.py
    alembic.ini
/docs
  design.md                   → original high-level design doc
  decisions/                  → one file per significant architecture decision (ADR-style), optional but recommended
/docker-compose.yml
CLAUDE.md
plan.md
```

---

## Conventions

- **API routes:** versioned, `/api/v1/<module>/...`, each module exposes a FastAPI `APIRouter` registered in `main.py` — don't add routes directly to the app object.
- **Module folder** (`app/<module>/`) contains its own `models.py` (SQLAlchemy models), `schemas.py` (Pydantic), `service.py` (business logic), and `router.py` (HTTP layer). Routers call services; services use models; nothing outside the module imports another module's `models.py` directly.
- **Database schema:** each module's tables live in their own Postgres schema (e.g. `irrigation.*`) — set via SQLAlchemy's `__table_args__ = {"schema": "irrigation"}` (or equivalent) on that module's models.
- **Frontend module = a Razor Class Library** under `/web-modules`, named `Farm.Web.<Module>`. It owns its own `.razor` pages/components, routes, and any module-specific state/services. Only `Farm.Web.Core` is referenced eagerly by the host; every other module is registered for **lazy loading** in `App.razor`'s router config so its assembly only downloads when a matching route is hit.
- **Frontend ↔ backend module naming stays aligned** — `Farm.Web.Irrigation` talks to `/api/v1/irrigation/...`, and so on. If you rename one, rename the other in the same change.
- **No cross-module component references** on the frontend beyond `Farm.Web.Core` — a feature module can use Core's shared components (layout, auth state, common UI) but not another feature module's components directly. Shared-but-not-core UI belongs in Core, not borrowed peer-to-peer.
- **Tests:** calculation-engine and service-layer logic gets unit tests (pytest); aim for coverage on anything that decides "how much water" or "when maintenance is due" — this is where real-world cost lives.
- **Commits:** reference the module and phase, e.g. `irrigation: add ET0 calculation engine (Phase 1)`.
- **Migrations:** single Alembic history for the whole project (Alembic doesn't natively do one-history-per-module) — but name revisions with the module prefix, e.g. `irrigation_add_calculation_run`, so the history stays readable as modules multiply. Never edit a migration that's already been applied anywhere outside local dev — write a new one.

---

## Current Status

> Update this section every session — it's the fastest way for an AI assistant (or future you) to know where things stand without re-reading the whole history.

- **Current phase:** Phase 1 — Foundation + Irrigation
- **What exists:** FastAPI backend (`apps/api`) with a working **Core: Users + Auth** module — JWT login/`/me` (`app/core/auth.py`, `security.py`, `router.py`), `core.users` table (SQLAlchemy + Alembic migration `core_add_users_table`), roles (owner/manager/worker — schema supports all three, only manager is actively used for now, see the Auth callout above), a CLI seed script for the first account (`python -m app.core.seed_admin`), CORS for local dev; Blazor WASM host (`apps/web`) + `Farm.Web.Core` RCL now wired to real auth — `AuthorizeRouteView`/`[Authorize]` route protection, `FarmAuthStateProvider` + `IAuthService` (JWT stored in `localStorage`), working Login/Sign-out, Dashboard's Owner/Worker toggle is role-driven (workers locked to Worker view, owners/managers keep the toggle) though worker accounts aren't created in practice yet; English/Afrikaans localization wired for the chrome (nav, shell, login, dashboard), switchable via a nav-bar language toggle, persisted client-side in `localStorage`; `Farm.Web.Irrigation` RCL (lazy-loaded, proves the plugin pattern) with 27 client-side calculators at `/irrigation/run-calculator` (formulas verified against irrigation.wsu.edu and watertankcalculator.com sources — see plan.md Decisions Log 2026-07-14), backed by the standalone zero-dependency `Farm.Irrigation.Calculators` C# library (metric units, 37 xunit tests) — not yet localized
- **What's in progress:** —
- **Next concrete step:** Core: Farm → Field/Block → Zone registry (CRUD + UI), then Irrigation backend module (data model + engine.py for authoritative, logged calculation runs — the client-side C# calculators stay for instant feedback). Also pending: per-user language preference persistence (backend), now that Users exist — see plan.md backlog. Worker role/views and multi-manager account creation UI are deferred (see plan.md Parking Lot).

---

## How to Work With This Repo (for AI assistants)

- Read `plan.md` first for the current task list and what's actively being worked on.
- Read this file for architecture rules and current stack — don't suggest a different stack/pattern without flagging it as a proposed change.
- When adding a module, follow the pattern in "Repo Structure" and "Architecture Rules" above.
- If you make an architectural decision (e.g. picking Prisma over Drizzle, adding a new module), update this file and `plan.md` in the same session — don't leave them stale.
