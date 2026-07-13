# CLAUDE.md

This file gives Claude (or any AI assistant working on this repo) persistent context about the project. Keep it up to date as decisions change — it should always reflect *current* reality, not history (history goes in `plan.md` or commit messages).

---

## Project Summary

A farm management platform, starting with irrigation calculations and growing into full farm operations: assets/equipment, crop records, tasks, finance, reporting.

- **Users:** owner + a few workers/family members, with different roles (owner / manager / worker)
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
| Auth | JWT sessions (e.g. `python-jose` or `pyjwt` + `passlib`), roles stored in DB |
| Background jobs | APScheduler in-process (upgrade to Celery + Redis only if justified) |
| Packaging | Docker Compose: `app`, `db` |
| Reverse proxy | Caddy |

> Prisma's Python client was considered but is unmaintained (deprecated March 2025, following Prisma's core rewrite from Rust to TypeScript) — not used for that reason.

> Frontend is C#/Blazor WASM rather than React so the whole stack stays statically typed and each feature module can be a real compiled unit (Razor Class Library) rather than just a folder convention. Trade-off accepted: true drop-in-without-rebuild plugin loading is harder in .NET's assembly model than in JS — modules are lazy-loaded at runtime but still need to be referenced by the host project at build time.

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
- **What exists:** repo scaffold — FastAPI backend (`apps/api`, SQLAlchemy + Alembic init, `core` schema, health endpoint only, no real auth/domain logic yet) and Blazor WASM host (`apps/web`) + `Farm.Web.Core` RCL (shared shell/nav, design tokens, Login and Dashboard screens, UI-only — not wired to real auth)
- **What's in progress:** —
- **Next concrete step:** Core: Users + Auth (JWT, roles), Farm → Field/Block → Zone registry (CRUD + UI), then Irrigation module (data model + calculation engine)

---

## How to Work With This Repo (for AI assistants)

- Read `plan.md` first for the current task list and what's actively being worked on.
- Read this file for architecture rules and current stack — don't suggest a different stack/pattern without flagging it as a proposed change.
- When adding a module, follow the pattern in "Repo Structure" and "Architecture Rules" above.
- If you make an architectural decision (e.g. picking Prisma over Drizzle, adding a new module), update this file and `plan.md` in the same session — don't leave them stale.
