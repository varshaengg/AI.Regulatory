# AI Regulatory Assistant — Wireframes & Persona Journeys

**Version**: v0.1 (aligned to SDD v1.5)
**Design language**: Microsoft **Fluent 2 for Web** (`@fluentui/react-components` v9)
**Reference set inspected**: `WireframeSamples/*.png` (Veeva RIM look — used to understand domain vocabulary and screen density, NOT visual style; ARA uses Fluent 2 tokens)

Open [`index.html`](./index.html) in any modern browser to view the clickable prototype (14 screens, no external dependencies).

---

## Personas

| Persona | Name (representative) | Primary goals in ARA |
|---|---|---|
| **Admin** | John — Global RA Systems Owner | Onboard CTD templates per module × country. Configure per-module source defaults for projects. Manage users and roles. |
| **RA Lead** | Priya — Regulatory Affairs Lead | Own product portfolio. Initiate dossier runs. Choose per-module overrides at run time. Track submission calendar. |
| **RA Author** | Meera — Regulatory Author | Review slot assignments. Resolve NeedsReview and Gap items. Use Copilot to find near-misses. Upload replacements. |
| **RA Reviewer / QA** | David — Regulatory QA | Verify compiled dossier and gap report. Approve or reject sign-off. Own audit-trail integrity. |

---

## End-to-end journey (persona swimlanes)

```
Admin ────► Setup templates + per-module sources
                          │
                          ▼
RA Lead ────► Initiate run ──► Choose per-module overrides ──► Submit
                                                                 │
                                                                 ▼
System  ────► Resolve templates ─► Discover ─► Extract ─► Classify ─► Assign ─► Analyze gaps
                                                                                    │
                                                                                    ▼
RA Author ─► Review assignments ◄─────► Copilot (RAG) ◄─────── Upload replacements
                          │
                          ▼
System  ────► Compile dossier (folder tree + PDF + Word + manifest + gap report)
                          │
                          ▼
RA Reviewer ─► Verify package ─► Sign-off (or reject with reason)
                          │
                          ▼
RA Lead ────► Download package or push to submission gateway
```

---

## Screen inventory

| # | Screen ID | Persona | Purpose |
|---|---|---|---|
| 01 | **A1 — Admin home** | Admin | Catalog health, users, licence status |
| 02 | **A2 — CTD Template Catalog** | Admin | Browse templates by region / country / module |
| 03 | **A3 — Upload Template** | Admin | Add a new per-module template with metadata |
| 04 | **A4 — Project Sources** | Admin / RA Lead | Configure per-module default source locations |
| 05 | **L1 — Home dashboard** | RA Lead | My active runs, upcoming submissions, alerts |
| 06 | **L2 — Projects list** | RA Lead / Author | Portfolio view with status chips |
| 07 | **L3 — Prepare Dossier · Basics** | RA Lead | Wizard step 1: project + country + submission type |
| 08 | **L4 — Prepare Dossier · Modules** ⭐ | RA Lead | Wizard step 2: per-module template & source overrides + direct upload |
| 09 | **L5 — Prepare Dossier · Review** | RA Lead | Wizard step 3: preview effective config, submit |
| 10 | **L6 — Run detail (live)** | RA Lead / Author | Progress by phase and by module with live counts |
| 11 | **U1 — Review Assignments** ⭐⭐ | RA Author | Module tree + slot inspector + Copilot side panel |
| 12 | **U2 — Copilot chat (focused)** | RA Author | Full-panel view for Copilot conversation with citations |
| 13 | **R1 — Compiled Package** | RA Reviewer | Assembled dossier browser + preview + gap-report link |
| 14 | **R2 — Sign-off** | RA Reviewer | Final approval dialog with mandatory rationale |

⭐ Key screens.

---

## Fluent 2 tokens applied

| Concern | Token / value |
|---|---|
| Typography | Segoe UI Variable, weights 400/500/600/700; size ramp 12 / 14 / 16 / 20 / 24 / 28 / 32 |
| Radius | 4 (medium) / 6 (large) / 8 (xLarge) |
| Elevation | 2 / 4 / 8 / 16 shadow ramp |
| Spacing grid | 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 |
| Brand | `#0F6CBD` primary, `#115EA3` hover, `#0C3B5E` pressed |
| Neutrals | `#FFFFFF` / `#FAFAFA` / `#F5F5F5` / `#F0F0F0` / `#E0E0E0` / `#D1D1D1` |
| Text | `#242424` / `#424242` / `#616161` / `#707070` |
| Semantic | `#107C10` success · `#F7630C` warning · `#C50F1F` danger · `#0078D4` info |
| Motion | 100 / 150 / 200 ms; `curveEasyEase` `cubic-bezier(.33,0,.67,1)` |
| Icons | Fluent System Icons (24px filled/regular set) |

---

## Component patterns used (Fluent React v9 mapping)

| Wireframe element | Fluent component |
|---|---|
| Top app bar | Custom + `Persona`, `SearchBox`, `Menu`, `Avatar` |
| Left rail | `NavDrawer` (compact + expanded), `NavItem`, `NavSectionHeader` |
| Cards | `Card`, `CardHeader`, `CardFooter` |
| Tables / grids | `DataGrid` (with `DataGridHeader`, `DataGridRow`, `DataGridCell`) |
| Wizard steps | Custom stepper (Fluent lacks a native one — use `Divider` + `Text` + numeric badges) |
| Tabs | `TabList` + `Tab` |
| Chips / tags | `Badge` (filled / outline) |
| Buttons | `Button` (appearance="primary" / "subtle" / "outline" / "transparent") |
| Chat | Custom `MessageBar`-inspired bubble list + `Textarea` + `Button` |
| Module tree | `Tree` (v9) with lazy loading |
| File upload | `input[type=file]` styled + `Button` |
| Dialogs | `Dialog`, `DialogSurface`, `DialogTitle`, `DialogContent` |
| Toast / status | `Toast`, `ToastTitle`, `ToastBody` |

---

## What these wireframes intentionally omit

- **Pixel-final visual design** — this is layout + hierarchy + behaviour, not the final skin. Visual designer produces the high-fidelity comps.
- **Motion specification** — noted at token level; animation storyboards live separately.
- **Empty / error / loading states** — each screen shows the "happy path"; empty and error states will be added as v0.2 delta.
- **Mobile / tablet breakpoints** — the SDD calls out 3 breakpoints (`>=1440`, `>=1024`, `>=768`). Wireframes here show the `>=1440` desktop breakpoint only; tablet responsive variants come next.
- **Accessibility overlay** — components chosen are all WCAG 2.1 AA compliant when using Fluent v9; focus-ring and screen-reader annotations will accompany final designs.

---

## How to review

1. Open `index.html` in Edge / Chrome / Firefox.
2. Use the left navigator to jump between screens (grouped by persona).
3. Each screen has a **caption strip at the top** describing the persona, journey step, and design intent.
4. Comments / redlines welcome as GitHub issues tagged `wireframe`.

---

## Next revisions planned

- **v0.2** — empty / error / loading state variants; tablet breakpoint
- **v0.3** — dark theme variant (Fluent v9 supports both out of the box)
- **v0.4** — high-fidelity mockups in Figma (this doc becomes the spec input)
