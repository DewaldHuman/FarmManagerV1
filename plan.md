# plan.md

Living roadmap and task tracker. Update this as work happens — check off tasks, add new ones, move things between phases if priorities shift. This file answers "what's next?"; `CLAUDE.md` answers "how do we build it?".

---

## Status Snapshot

- **Active phase:** Phase 1 — Foundation + Irrigation
- **Last updated:** 2026-07-13
- **Blockers:** none
- **Just landed:** repo scaffold (FastAPI backend + Blazor WASM host/Farm.Web.Core RCL), shared shell/nav + design tokens, Login and Dashboard screens; 15 irrigation calculators in a lazy-loaded `Farm.Web.Irrigation` module backed by the standalone `Farm.Irrigation.Calculators` C# library — see Decisions Log

---

## Phase 1 — Foundation + Irrigation

Goal: running end-to-end on the farm PC via Docker Compose — auth, farm/field/zone setup, and irrigation calculations working for real zones.

- [x] Scaffold repo (FastAPI backend, Blazor WASM host + Farm.Web.Core RCL, docker-compose.yml)
- [x] Frontend: set up lazy-loading assembly registration in host's `App.razor` (proves the plugin pattern before other modules exist) — done: `Farm.Web.Irrigation` + `Farm.Irrigation.Calculators` lazy-load on first `/irrigation/*` navigation (verified via network trace)
- [x] Frontend: build shared shell/nav + design tokens as Razor Components, per `docs/design-ui/README.md` (style guide section) — this is the foundation every module's screens sit inside
- [x] Frontend: recreate Login, Dashboard (owner/manager + worker toggle) as Razor Components per `docs/design-ui/README.md` — desktop only for Phase 1
- [x] Frontend: Run Calculation screen — built in `Farm.Web.Irrigation` with the design's picker-first flow, expanded from 3 to 15 calculators (see Decisions Log 2026-07-13, calculators)
- [ ] Frontend: recreate Zone List, Farm Structure CRUD, Calculation History as Razor Components — not yet built
- [x] Backend: set up SQLAlchemy engine/session, Alembic init pointed at Postgres
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
- 2026-07-13 — Scaffolded repo: FastAPI backend (`apps/api`, SQLAlchemy engine/session + Alembic init, `core` schema, health endpoint) and Blazor WASM host (`apps/web`, `Farm.Web.Host`) + `Farm.Web.Core` RCL, wired via a solution file. Shared shell/nav (`AppShellLayout`, `NavMenu`) and design tokens (CSS custom properties matching `docs/design-ui/README.md` exactly) live in `Farm.Web.Core`; host's `App.razor` routes to Core's layout directly rather than keeping its own `/Shell` folder, since the ask was for shell components to live in Core. Login (`BlankLayout`, no nav) and Dashboard (owner/manager + worker toggle) built and verified against the design reference in-browser — colors/spacing/copy match. Lazy-loading assembly registration in `App.razor` deferred until an actual lazy module (Farm.Web.Irrigation) exists to prove the pattern against.
- 2026-07-13 — Found `docs/design.md` byte-identical to `docs/design-ui/README.md` (contains the UI handoff doc, not the original architecture design doc CLAUDE.md references). Flagged to user, left as-is pending their decision on how to restore the real content.
- 2026-07-13 — **Calculators:** built 15 client-side irrigation calculators (curated union of watertankcalculator.com/calculators/agriculture and irrigation.wsu.edu Select-Calculators; skipped livestock, chemigation, weir/flume measurement, center-pivot and residential ones as out of scope). Pure math lives in `web-modules/Farm.Irrigation.Calculators` — a zero-dependency C# class library (metric/SA units: mm, ha, m³/h, kPa, kW) with xunit coverage (`Farm.Irrigation.Calculators.Tests`, 18 tests) — deliberately standalone so it can be pulled out or replaced. UI is `web-modules/Farm.Web.Irrigation` (RCL, lazy-loaded) at `/irrigation/run-calculator`, schema-driven forms per calculator, grouped picker (Run time & volume / Application & scheduling / System design). **Note:** these run client-side in C# for instant feedback; the Python backend `irrigation/engine.py` remains the plan for *authoritative, logged* calculation runs (the "Confirm & Log Run" button ships disabled until then). Kc values, soil AWC and pipe C presets are generic placeholders in `Presets.cs` — swap for farm-specific values when Core Settings lands.
- 2026-07-14 — **Calculators v3 (watertankcalculator re-check → 27):** re-verified all 9 watertankcalculator.com agriculture pages against the build; the 7 already implemented (Irrigation Water Requirement, Drip Tank Size, Sprinkler Water Usage, Livestock Water, Pump Size, Field Irrigation Tank, Greenhouse Water Use) all match. Added the 2 that were missing: **Crop Water Need** (FAO-56 ETc=Kc×ET₀, volume = ETc×ha×10×days — pure agronomic demand, no system losses, distinct from ET₀ Run-Time) and **Farm Water Storage** (combined livestock + irrigation daily demand × reserve days; either component may be zero for crop-only or livestock-only farms). New file `DemandCalculators.cs`; tests now 37, all passing; browser-verified Crop Water Need (917 m³/7 days) and Farm Water Storage (613.5 kL). Greenhouse "× efficiency" prose on the source is really ÷ efficiency per its own multiplier table (÷0.95 drip etc.) — build divides, confirmed correct.
- 2026-07-14 — **Calculators v2 (formula verification + expansion to 24):** user supplied exact source links; fetched every page and cross-checked each formula. All 15 existing implementations verified correct against the published sources — FAO-56 chain (ETc = Kc×ET₀, IRn = ETc−Peff, IRg = IRn÷Ea, m³ = mm×ha×10), WSU Irrigation Frequency (F = AWC×Rz×MAD÷ETc), the metric application-rate identities (WSU's 96.3 / 1.604 imperial constants are the same relation), Hazen-Williams (10.67 SI form, checked against pipe tables), Hydraulic Institute pump power (Q×H/367). Zero math fixes needed. Added 9 new calculators with published formulas: Sprinkler Water Usage, Sprinkler Density (rect 10 000/(S×R), triangular √3/2 geometry), Catch-Can Test, Lateral/Outlet Friction Loss (Christiansen F = 1/(m+1)+1/(2N)+√(m−1)/(6N²), m=1.852 — F(20)=0.376 matches tables), Field Area (4 shapes), Required System Capacity (WSU A×d÷(interval×hours×eff)), Field Irrigation Tank Size, Drip Tank Size, Greenhouse Water Use, Livestock Water (FAO/USDA rates in `Presets.cs`). Pump Power upgraded to full Pump Sizing (motor efficiency + safety factor → next standard IEC motor kW). Picker now 4 categories (Run time & volume / Application & scheduling / Pipes & pumps / Land, storage & supply); pipe material list extended (galvanized, aluminium w/ couplers, both C=120). Test suite now 34 tests, all passing; browser-verified Pump Sizing (15 kW), Lateral Loss (27.7 kPa, F=0.38), Livestock (5 625 L/day), Field Area circle (3.14 ha). **Skipped with reasons:** Fitting Pressure Loss (WSU publishes no formula/table — image only, nothing to verify against), Drip Design for Landscapes (US-residential empirical climate factors), Required Maximum Flow Rate (subset of Required System Capacity).

---

## Parking Lot (ideas not yet scheduled)

- Weather API integration (replace manual entry) — Open-Meteo or local station feed
- **Mobile/tablet UI for workers** — deferred from the Phase 1 design pass (see Decisions Log, 2026-07-13); needs its own design pass with Claude Design before/during worker rollout, not just a CSS breakpoint afterthought
- Soil moisture sensor integration
- Multi-farm support (if ever relevant)
