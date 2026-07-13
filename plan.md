# plan.md

Living roadmap and task tracker. Update this as work happens — check off tasks, add new ones, move things between phases if priorities shift. This file answers "what's next?"; `CLAUDE.md` answers "how do we build it?".

---

## Status Snapshot

- **Active phase:** Phase 1 — Foundation + Irrigation
- **Last updated:** 2026-07-13
- **Blockers:** none

---

## Phase 1 — Foundation + Irrigation

Goal: running end-to-end on the farm PC via Docker Compose — auth, farm/field/zone setup, and irrigation calculations working for real zones.

- [ ] Scaffold repo (FastAPI backend, Blazor WASM host + Farm.Web.Core RCL, docker-compose.yml)
- [ ] Frontend: set up lazy-loading assembly registration in host's `App.razor` (proves the plugin pattern before other modules exist)
- [ ] Frontend: build shared shell/nav + design tokens as Razor Components, per `docs/design-ui/README.md` (style guide section) — this is the foundation every module's screens sit inside
- [ ] Frontend: recreate Login, Dashboard (owner/manager + worker toggle), Zone List, Farm Structure CRUD, Run Calculation (3-calculator flow), Calculation History as Razor Components per `docs/design-ui/README.md` — desktop only for Phase 1
- [ ] Backend: set up SQLAlchemy engine/session, Alembic init pointed at Postgres
- [ ] Core: Users + Auth (login, JWT, roles: owner/manager/worker)
- [ ] Core: Farm → Field/Block → Zone registry (CRUD + basic UI)
- [ ] Core: Settings (units, timezone, default crop coefficients)
- [ ] Irrigation: data model (IrrigationZone, IrrigationSystem, Schedule, WaterSource, CalculationRun)
- [ ] Irrigation: calculation engine (pure library) — ET₀, crop coefficient adjustment, net irrigation requirement, run-time from flow rate
- [ ] Irrigation: manual weather input (API integration deferred to Phase 1.5)
- [ ] Irrigation: UI to run a calculation and view/log results per zone
- [ ] Seed script with real farm's fields/zones
- [ ] Docker Compose running locally, accessible over LAN
- [ ] Backup script (`pg_dump` on schedule)
- [ ] Basic unit tests for calculation engine

**Definition of done:** you can log in from a device on the LAN, pick a zone, run an irrigation calculation using real inputs, and get a sensible recommended run-time — with the result logged for later review.

---

## Phase 2 — Assets

- [ ] Assets module: Asset, AssetCategory, MaintenanceLog, ServiceSchedule
- [ ] Link assets to Field/Zone (location)
- [ ] Maintenance due-date tracking + notification
- [ ] File attachments (manuals, invoices) via Core file storage

## Phase 3 — Crops + Tasks

- [ ] Crops module: Crop, Planting, GrowthStage, HarvestRecord
- [ ] Feed growth stage into Irrigation's crop coefficient lookup
- [ ] Tasks module: Task, Assignment, TimeLog — assign work to workers, link to any module entity

## Phase 4 — Finance + Reporting

- [ ] Finance module: Expense, Income, Budget — link to asset/field/crop
- [ ] Reporting: cross-module dashboards (cost per field, water use trends, maintenance history)

## Phase 5 — Cloud Migration

- [ ] Move containers to cloud VM or managed hosting
- [ ] Managed Postgres (replace containerized DB)
- [ ] Proper domain + HTTPS via Caddy/Traefik
- [ ] Revisit auth for internet-facing exposure
- [ ] Multi-farm support (only if actually needed)

---

## Decisions Log

> One line per significant decision, newest first. If a decision needs more explanation, link to a file in `docs/decisions/`.

- 2026-07-13 — Chose modular monolith over microservices (single-farm LAN scale doesn't justify the ops overhead)
- 2026-07-13 — Initially chose NestJS + React + Postgres as stack
- 2026-07-13 — Switched backend to Python (FastAPI) per preference; considered Prisma for ORM but its Python client is unmaintained (deprecated March 2025) — chose SQLAlchemy 2.x + Alembic instead (see CLAUDE.md for full table)
- 2026-07-13 — Switched frontend to Blazor WebAssembly (C#), modules as lazy-loaded Razor Class Libraries — mirrors the backend's module pattern more directly than a JS SPA would
- 2026-07-13 — Received Phase 1 UI design from Claude Design (`docs/design-ui/`) — desktop-only; mobile/tablet for worker use was explicitly deferred, not designed in this pass. Decision: proceed with desktop-only build for Phase 1, revisit mobile design before/during worker rollout

---

## Parking Lot (ideas not yet scheduled)

- Weather API integration (replace manual entry) — Open-Meteo or local station feed
- **Mobile/tablet UI for workers** — deferred from the Phase 1 design pass (see Decisions Log, 2026-07-13); needs its own design pass with Claude Design before/during worker rollout, not just a CSS breakpoint afterthought
- Soil moisture sensor integration
- Multi-farm support (if ever relevant)
