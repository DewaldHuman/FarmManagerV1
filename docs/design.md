# Handoff: Farm Platform UI — Phase 1 (Core + Irrigation)

## Overview
UI design for Phase 1 of the farm management platform: auth, farm/field/zone registry, and irrigation calculations. Covers the shared navigation shell plus the concrete Phase 1 screens, designed to prove the shell scales to future modules (Assets, Crops, Tasks, Finance, Reporting) without redesign.

**Note:** `farm-platform-design.md` (referenced in the original design prompt as the source system-design doc) was not present in this project's uploads — only `CLAUDE.md` and `plan.md` were available and used as design input. If that file exists elsewhere, reconcile it against this README before implementing; nothing here should contradict its architecture, since none of it was consulted.

## About the Design Files
The file in this bundle (`Farm Platform.dc.html`) is a **design reference built in HTML** — a prototype of look, layout, and interaction, not production code. It targets a Blazor WebAssembly frontend (per `CLAUDE.md`), so implementation means **recreating these screens as Razor Components** in `Farm.Web.Core` / `Farm.Web.Irrigation`, using Blazor's own patterns (component params, `EventCallback`, `@bind`) — not embedding or porting the HTML/JS directly.

## Fidelity
**High-fidelity.** Colors, type sizes, spacing, and copy are final-intent, not placeholders. Treat exact hex values and pixel sizes below as the target, not a rough guide.

## Scope note
Desktop only. Mobile/tablet layouts for workers were explicitly deferred — not designed in this pass. Flag this back before implementing anything worker-facing on a handheld device.

## Screens / Views

### 1. Style Guide (reference sheet, not a shipped screen)
Documents the palette, type scale, buttons, and status pill styles used throughout. Use it as the single source of truth for design tokens (below) rather than re-deriving values from each screen.

### 2. Login
- Two-column layout, full-bleed dark green left panel (brand/value prop copy) + white right panel (form), rounded 20px container, max-width 1400px.
- Left: eyebrow label, H2 headline (40px/700), supporting paragraph (17px), all in white/light green text on `#121e12` background.
- Right: "Sign in" H1 (28px/700), labeled text/password inputs (18px padding, 2px border, 10px radius), full-width primary button, helper text explaining role-based redirect (owner/manager → farm overview, worker → Today view).

### 3. Dashboard (role-based)
Shared shell (left nav, see below) + a view-mode toggle (segmented control, top right) switching between:
- **Owner/Manager view:** 4-column stat cards (Active zones, Due today, Overdue, Runs logged this week — each tinted by tone: neutral/due/overdue), followed by a "Recent activity" list (flat rows, timestamp right-aligned).
- **Worker view:** "What needs doing today" — a vertical stack of large, color-tinted task cards (zone name, crop, status pill, big "Run" button ≥44px tall). No stats, no history — deliberately sparse.

### 4. Irrigation Zone List
Shell + 2-column grid of zone cards. Each card: zone name (20px/700), field + crop subtitle, status pill (top right), and a 2-column stat row (last watered, area). Card background and border are tinted by status (on schedule = green, due = amber, overdue = red). Cards link to Run Calculation.

### 5. Farm Structure (Farm → Field → Zone CRUD)
Shell + vertical list of field cards. Each field card: field name, Edit + "+ Add Zone" buttons (top right), and a wrapped row of zone chips underneath. Top-level "+ Add Field" button in the page header.

### 6. Run Irrigation Calculation
Shell + a **picker-first flow**:
- Default state: "Choose a calculator" — 3 cards side by side (ET₀ Run-Time, Manual Volume, Drip Emitter Sizing), each with icon, label, one-line description. Clicking a card selects it.
- Selected state: a "← Choose a different calculator" back button, then a 2-column layout — left = that calculator's input form (zone select, calculator-specific fields, all large/touch-friendly), right = a sticky Result panel (empty-state dashed card until "Run Calculation" is clicked, then a tinted result card with the headline number + a "Confirm & Log Run" button).
- Each calculator type has distinct inputs:
  - **ET₀ Run-Time:** zone, crop growth stage (3-way segmented control: Establishment / Mid-season / Late season), reference ET₀ (mm/day), rainfall since last run (mm).
  - **Manual Volume:** zone, target volume (L), flow rate (L/min).
  - **Drip Emitter Sizing:** zone, emitter count, flow per emitter (L/hr), target volume (L).
- All three currently share one result state/demo values in the prototype — real implementation needs each to compute its own result independently.

### 7. Calculation History
Shell + a flat table: Date, Zone, Growth stage, Run-time, Volume, Status (pill). Header row in uppercase/muted, rows separated by 1px hairlines.

## Navigation Shell (all screens)
- Fixed 240px left rail, dark green (`#0f190f`) background, farm name/icon at top.
- One row per module (Irrigation, Assets, Crops, Tasks, Finance, Reporting). Only Irrigation is active; the rest show a "Soon" tag and muted text — proves the shell scales without redesign.
- **Irrigation is expanded with sub-nav**: Zones, Run Calculator, Zone Designs, History — indented under the parent item. Other modules have no sub-nav since they're not built yet; when a module goes live, give it the same sub-nav treatment.
- Active item/sub-item: filled pill (`#18361a` background, white text). Inactive sub-items: muted light-green text, transparent background.

## Interactions & Behavior
- Dashboard view toggle: click switches between Owner/Manager and Worker content, no navigation/reload.
- Run Calculation: click a calculator card → shows that calculator's form; back button returns to the picker; "Run Calculation" reveals the Result panel (currently instant — no loading state modeled, add one for a real async backend call).
- Zone cards on the Zone List screen link to Run Calculation (anchor jump in the prototype — a real route in the app).
- No animations/transitions beyond default browser behavior — nothing here depends on a JS animation library.
- No responsive/mobile behavior defined (see Scope note).

## State Management (per screen)
- Dashboard: `viewMode` ('owner' | 'worker').
- Run Calculation: `selectedCalculator` (null | 'et0' | 'manual' | 'drip'), per-calculator form fields, `result` (null until calculated).
- Zone List / Farm Structure: read from the farm/field/zone registry (Core module) — no local-only state beyond form inputs on Add/Edit actions (not designed in this pass — only entry points/buttons are shown).

## Design Tokens

**Color** (hex, converted from the original oklch design values — same hues/lightness, safe to use as-is):
- Background: `#f7f5ec` · Surface/white: `#ffffff` · Ink (primary text): `#221f14`
- Secondary text: `#595549` · Meta/muted text: `#676357`
- Primary green (actions): `#2c6330` (hover/pressed: `#17501d`)
- Earth brown (secondary accent): `#6e4d32`
- Shell background (dark green): `#0f190f` · Shell active item: `#18361a`
- Borders: light `#e1ded3`, medium `#d2cebf`
- Status — On schedule: bg `#e6f8e6` / border `#b6d9b6` / badge bg `#c2ebc2` / text `#0b3e12`
- Status — Due: bg `#fef4df` / border `#edd0a0` / badge bg `#fed899` / text `#663400`
- Status — Overdue: bg `#ffeeeb` / border `#ffc4bd` / badge bg `#ffc4bd` / text `#861118`
- Result panel (success green): bg `#dbf8da` / border `#9ecc9e` / heading `#1e4e22` / value `#003306`

**Typography:** system sans stack (`-apple-system, "Helvetica Neue", Helvetica, Arial, sans-serif`). Scale used: 48px/700 (result numbers), 36–40px/700 (page/hero headlines), 32px/700 (H1), 22–28px/700 (H2/section), 19–20px/700 (card titles), 15–17px/400–600 (body/labels/inputs), 13–14px/600–700 uppercase tracked (eyebrows, table headers, meta).

**Spacing/radius:** page padding 40–48px, card padding 24–32px, card radius 14–20px, buttons/inputs radius 8–12px, pill radius 999px, gaps 12–20px in grids.

**Buttons:** primary = filled `#2c6330` / white text, 16–20px padding, 10–12px radius; secondary = white bg / 2px `#d2cebf` border / dark text.

## Assets
No images or icons beyond inline emoji used as module glyphs in the nav (💧🔧🌱✅💰📊) and dashboard (🌾). No external icon library — swap these for the codebase's existing icon set if one exists; nothing here depends on emoji specifically.

## Files
- `Farm Platform.dc.html` — the full design reference (style guide + all 6 screens). Open directly in a browser to view/interact with it.
