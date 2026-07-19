# Figma Make prompt — ARA Wireframes

Paste the block below into **Figma Make** (New file → Make → paste as the initial prompt). Generate at 1440 × 900 desktop frames. After the first pass, you can ask Figma Make to iterate on any single screen using its ID (e.g. "Refine screen L4 — Modules step of the dossier wizard").

---

## PROMPT (copy from here ⬇)

Design a **desktop web application** called **ARA — AI Regulatory Assistant**. It helps pharmaceutical Regulatory Affairs teams prepare EU **CTD (Common Technical Document) submission dossiers** (Modules 1–5) with the help of AI agents that draft each section from approved source content and CTD template documents.

Produce **14 high-fidelity wireframe frames at 1440 × 900**, one per screen listed below. Use a light theme.

### Design system (use exactly these tokens)

Follow **Microsoft Fluent 2 for Web**. Typography = **Segoe UI Variable** (fallback Segoe UI). Base font 14 px, line-height 1.4. Radii: 4 / 6 / 8. Spacing scale: 4, 8, 12, 16, 20, 24, 32, 40.

Colors:
- Brand primary `#0F6CBD`, hover `#115EA3`, pressed `#0C3B5E`, tint `#EBF3FA`
- Neutrals: `#FFFFFF`, `#FAFAFA`, `#F5F5F5`, `#F0F0F0`, `#E0E0E0`, `#D1D1D1`, `#ABABAB`
- Text: primary `#242424`, secondary `#424242`, tertiary `#616161`, disabled `#707070`
- Semantic: success `#107C10` (tint `#DFF6DD`), warning `#F7630C` (tint `#FFF4CE`), danger `#C50F1F` (tint `#FDE7E9`)
- Persona badges: Admin `#EEE0FF` on `#5C2E91`; RA Lead `#EBF3FA` on `#0C3B5E`; RA Author `#DFF6DD` on `#0B5A0B`; RA Reviewer `#FFF4CE` on `#8A6100`

Shadows: elevation 4 = `0 2px 4px rgba(0,0,0,.10)`; elevation 8 = `0 4px 12px rgba(0,0,0,.12)`.

Components (Fluent 2 style): AppBar with product logo + tenant/env pill + user avatar; left NavigationRail 260 px wide; primary Button (filled brand), secondary (outline neutral), subtle (text brand); Input, Combobox, Dropdown, DatePicker, TagPicker (chip style), Checkbox, RadioGroup, Toggle; Tabs (underline), Breadcrumb, ProgressBar, Badge/Chip (pill), Toast, Dialog, Drawer, DataGrid with sticky header + zebra hover; Stepper (numbered circles connected by 2 px bars, filled brand for active, filled success for done).

### Personas (show a persona badge on every screen caption)

- **Admin (Sara)** — configures tenant: CTD templates, storage bindings by country/region, LLM & Search endpoints, roles.
- **RA Lead (Marcus)** — creates a dossier project, picks target country + product, launches the multi-agent run, monitors progress.
- **RA Author (Priya)** — reviews AI-drafted section content, edits inline, accepts/rejects citations, chats with the copilot.
- **RA Reviewer (Dr. Chen)** — sees the compiled package as a submission-ready view and signs off.

### Global chrome (top bar) on every screen

Left: 24 × 24 rounded brand square with "A" glyph, then "ARA" (semibold 13 px), a `·` separator, then the current section title in tertiary text. Right: a "Docs" secondary button and a 28 px circular avatar with the user's initials on brand-tint background.

### Left navigation rail (260 px) on every screen

Header: title "ARA · Wireframes" + subtitle "Fluent 2 · React SPA + BFF · v1.5". Four groups (uppercase 11 px labels): **Admin** (A1–A4), **RA Lead** (L1–L6), **RA Author** (U1, U2), **RA Reviewer** (R1, R2). Each item shows a 2-character screen ID in tertiary text followed by the label. Mark **L4** with a single ⭐, **U1** with ⭐⭐ (indicates highest fidelity). Active item = brand-tint background, brand-pressed text, semibold.

---

## 14 SCREENS — content specs

### A1 · Admin home — Tenant setup at a glance  (persona: Admin)
- H1 "Welcome back, Sara". Sub "Contoso Pharma · Tenant contoso.onmicrosoft.com · West Europe".
- Four stat cards in a row: **42** CTD templates, **18** Countries mapped, **6** Storage accounts, **2** Setup tasks pending.
- Two-column below. Left card **Setup checklist** with 4 rows, each row = colored dot + bold label + secondary hint + right-side status chip or "Fix →" link. Rows: Entra ID app registration (done), Azure OpenAI endpoint gpt-4o Sweden Central (done), AI Search index ara-eu empty · reindex required (warning), Storage binding · Germany · fallback to EU region (danger). Right card **Recent activity** = 4-row table (Template / Source / User / Key chip in col 1, description in col 2, relative time in col 3).

### A2 · CTD Template Catalog  (Admin)
- Breadcrumb: Admin › **CTD Templates**. H1 "CTD Template Catalog", secondary Text "Templates apply per country. Missing country falls back to region, then global default." Primary button "Upload template".
- Toolbar: search input with magnifier icon (placeholder "Search by country, module, version"), Combobox filters "Country · All", "Module · All", "Status · Active". Right-side toggle "Show archived".
- DataGrid columns: Country (flag emoji + code), Region, Modules covered (comma pills 1,2,3,4,5), Version, Uploaded by, Uploaded on, Status (chip: Active/Draft/Archived), row-hover actions (View · Replace · Archive). Populate 8 realistic rows: DE, FR, IT, ES, NL, EU (region default), Global (fallback), UK.
- Empty-state hint at the bottom in a subtle info note: "Countries with no dedicated template will inherit the EU region template."

### A3 · Upload Template  (Admin)  — modal dialog view
- Show the A2 grid faded in the background; centered 560 px dialog on elevation 16.
- Title "Upload CTD template". Fields: Country (Combobox with search), Region (auto-filled, disabled), Applies to modules (multi-select checkbox row 1 2 3 4 5), Version (input, hint "e.g. 4.2"), Effective date (DatePicker), File (dashed drop zone "Drag .docx here or **Browse**" — max 25 MB).
- Info banner (brand tint): "This template will override the region default for the selected country when a dossier project is created."
- Footer buttons right-aligned: Cancel (subtle), Upload (primary).

### A4 · Project Sources  (Admin)
- Breadcrumb: Admin › **Sources** › **Project PX-102**. H1 "Default sources · Project PX-102". Sub "Set the default source location for each CTD module. RA Leads can override these when creating a dossier run."
- Tabs: Sources | Team | Settings. Sources tab shown.
- Card list, one per module (M1 Administrative, M2 Summaries, M3 Quality, M4 Nonclinical, M5 Clinical). Each row: module icon + name, current source (grey pill "Azure Blob · contosopharma/px102/m3"), owner avatar stack, last synced timestamp, right-side "Edit source" secondary button and a "Test connection" subtle button with green dot when OK. M4 shows an empty state chip "Not configured" and a "Add source" primary button.

### L1 · Home dashboard  (RA Lead)
- H1 "Good morning, Marcus". Sub "3 active dossier runs · 2 need attention".
- Primary CTA "New dossier request" (large primary button top right).
- Grid: two large cards (My active runs, My assignments), plus a right rail (270 px) with **Notifications** feed.
- **My active runs** table: Project (link), Country (flag), Modules progress (mini stacked bar M1–M5 with per-module color), Overall progress (progress bar with %), Status chip (Drafting / Reviewing / Blocked), Last updated, quick actions.
- **My assignments** = 4 chips showing "Review 12 sections" style counters colored by urgency.
- Notifications rail: 5 items with icons (agent completed, section flagged, citation missing, template updated, comment mention).

### L2 · Projects list  (RA Lead)
- Breadcrumb: Home › **Projects**. H1 "Dossier projects". Primary button "New dossier request".
- Toolbar: search, filters (Country, Product, Status, Owner), view toggle Grid/List.
- DataGrid rows: Project name (link), Product code, Target country (flag), Modules (chips), Owner (avatar + name), Progress bar, Status chip, Created, Menu (⋯). 12 rows with varied statuses (Draft, In progress, Under review, Blocked, Submitted).

### L3 · New dossier — Basics  (RA Lead)  — Wizard step 1 of 3
- Stepper across the top: **① Basics** (active) — ② Modules — ③ Review & Launch.
- Two-column form. Left column fields: Project name, Product (searchable Combobox with product code + name), Product version, Target country (Combobox — flag + name + region hint), Submission type (Radio: Initial / Variation / Renewal), Target submission date (DatePicker), Owner (auto-filled current user, editable). Right column card: **Template preview** — shows which CTD template will apply based on country selection, with a "This is the EU region default template (no country-specific template for Germany)" note. Footer: Cancel (subtle), Next → (primary).

### L4 · New dossier — Modules  (RA Lead)  ⭐ HERO SCREEN — Wizard step 2 of 3
- Stepper: ① Basics (done) — **② Modules** (active) — ③ Review & Launch.
- Layout: left rail 220 px module picker + right pane details.
- **Module picker** — 5 cards stacked. Each shows M# in a colored circle, module title (M1 Administrative / M2 Summaries / M3 Quality / M4 Nonclinical / M5 Clinical), and a completion state chip (Configured / Needs input / Skipped). Selected card has brand-tint background.
- **Right pane** shows for the selected module (default M3 Quality):
  - Sub-heading "M3 · Quality — 12 sub-modules".
  - **Template**: default is inherited from A2. Show current pill "EU region v4.2" plus an "Override template for this module" secondary button that opens a drop zone.
  - **Source location**: Radio group — (a) Use project default `contosopharma/px102/m3` (selected), (b) Different location for this module (opens Combobox path picker), (c) I'll upload the content file directly (opens dashed drop zone accepting .docx / .pdf / .zip).
  - **Sub-module coverage** — DataGrid: Section code (3.2.S.1, 3.2.S.2 …), Section title, Included (Toggle), Source status (green "Found" / amber "Partial" / red "Missing"), Notes.
  - Info note at the bottom (brand tint): "Sections marked Missing will not be drafted unless you upload the content file above."
- Footer: ← Back (subtle), Save draft (secondary), Next → (primary).

### L5 · New dossier — Review & Launch  (RA Lead)  — Wizard step 3 of 3
- Stepper: ① Basics (done) — ② Modules (done) — **③ Review & Launch** (active).
- Read-only summary in three cards: **Basics** (with Edit link back to step 1), **Modules & Sources** (compact table of 5 rows: module, template used, source path, sub-modules included/total), **Assignments** (RA Authors auto-assigned per module with avatars + editable).
- Right rail card: **Run estimate** — LLM cost estimate ($4.20 – $6.80), estimated time (18–25 min), token budget bar.
- Warning note if any module has Missing sections: yellow "3 sections in M4 have no source and will be skipped".
- Footer buttons: Save as draft (secondary), Cancel (subtle), 🚀 **Launch dossier run** (primary large).

### L6 · Run detail — live  (RA Lead)
- Breadcrumb: Projects › **PX-102 · DE · Initial**. H1 "Dossier run #4821" + status chip "Running · started 12:04".
- Split layout (2fr / 1fr). Left = **Module progress board**: 5 module cards vertically stacked, each with progress bar, current agent activity ("Drafting section 3.2.S.4.2 …"), sub-section counts (12/24 done). One module shows a red "Blocked · citation missing" chip with "Assign to author" link.
- Right = **Live activity log** — dark terminal-style panel, timestamped colored lines: `12:04:12 [INFO] Orchestrator started`, `12:04:18 [OK] M1 Administrative complete`, `12:05:33 [WARN] M3.2.S.4.2 low confidence 0.62 — flagged for review`, etc. Auto-scroll indicator.
- Below the split: **Agents involved** = row of 5 agent chips (Orchestrator, Section Drafter, Citation Verifier, Compliance Checker, Compiler) with tiny status dots.

### U1 · Review Assignments  (RA Author)  ⭐⭐ HIGHEST FIDELITY
- No standard pane padding — this is a full-width workspace card 640 px tall.
- Three columns inside one bordered frame: **Assignments list (280 px)** | **Section editor (flex)** | **Copilot panel (340 px)**.
- **Assignments column**: header "12 assignments · 4 need attention" with filter icon. List of cards, each showing section code (e.g. `3.2.S.4.2`), section title, project · module meta line, and a chip strip (confidence Low/Med/High, "3 citations", "flagged"). Selected card has brand-tint background.
- **Editor column**: on a near-white page background, serif body font, with rendered section content. Include:
  - Heading `3.2.S.4.2 Analytical Procedures`.
  - Two paragraphs of realistic drug-substance analytical procedure text.
  - Inline yellow-highlighted `<mark>` phrase with warning-color underline indicating a low-confidence sentence.
  - Superscript citation numbers `¹ ² ³` in brand color, clickable.
  - Sub-heading `Validation of Analytical Procedures` followed by another paragraph.
  - Top of column shows a mini toolbar: Accept · Reject · Request revision · Add comment · a segmented control "Draft | Compare with source | History".
- **Copilot column** (grey background): header "Copilot" + model chip "gpt-4o". Show a short conversation:
  - User message (brand tint bubble): "Explain the flagged sentence about the LOD calculation."
  - Copilot reply (white bubble) with 3 lines of grounded explanation + a **Citation card** below reading "Source: PX-102_M3_QualityDataset.docx · p. 47 · §3.2.S.4.2". A second copilot reply suggesting a rewrite ("Would you like me to rewrite it with the ICH Q2(R2) reference?") with two inline buttons "Rewrite" and "Insert reference".
  - Footer: textarea "Ask about this section…" + send button.

### U2 · Copilot focused  (RA Author)
- Same author context but Copilot expanded to full width. Two panels: left = active section rendered read-only with the highlighted sentence still visible; right = large Copilot conversation with richer citation cards (image thumbnail of source doc page, page number, confidence gauge, "Open source" link). Bottom prompt bar with attachment button, slash-command hint `/rewrite  /shorten  /translate  /find-citation`.

### R1 · Compiled Package  (RA Reviewer)
- Breadcrumb: Projects › PX-102 › **Compiled package v1**. H1 "Compiled dossier · PX-102 · Germany · Initial submission".
- Two-column: left 320 px **Module tree** (collapsible: M1 › M2 › M3 › M4 › M5, each with sub-modules; icons for docx/pdf, unresolved-comment count badges). Right = **Document viewer** — page thumbnails strip on top, current page rendered below with header/footer preview. Right rail (280 px) card with **Package meta**: version, generated by run #4821, generated at, size, page count, section counts.
- Top-right actions: Download .zip (secondary), Export DOCX (secondary), Approve for sign-off (primary).
- Below the viewer: **Validation summary** row of chip counters: 214 sections ✅ · 4 warnings ⚠ · 0 errors ✖ · 2 unresolved comments.

### R2 · Sign-off  (RA Reviewer)
- Center 640 px card on a subtle patterned background. Big title "Sign off dossier PX-102 · DE · v1".
- Attestation block: rendered legal-style attestation paragraph (2 lines, serif). Below it, a signature capture area: pre-filled name (Dr. Anna Chen), title (Head of Regulatory Affairs), timestamp (now), method chip "Entra ID authenticated".
- Checkbox row: "I confirm that all reviewed sections meet the requirements of ICH M4 CTD guidance and internal SOP RA-018".
- Footer: Cancel (subtle), Save as pending (secondary), **Sign & lock package** (primary large).
- After-sign toast preview (dismissable) at bottom-right: green success "Package v1 signed and locked. Ready for eCTD publishing."

---

### Notes for Figma Make

1. Every screen should render its persona badge, screen ID (`A1`, `L4`, etc.) and a one-sentence "design intent" caption above the frame body.
2. Reuse the top AppBar and left NavigationRail as **components** so edits propagate.
3. Keep line lengths comfortable — table cells 12–13 px, section headings 15–16 px, page H1 22 px.
4. Use realistic pharma sample data: product code PX-102, drug name Elmiravir, country codes DE / FR / IT / ES / NL, section codes from ICH M4Q (3.2.S.*, 3.2.P.*), agent names Orchestrator / Section Drafter / Citation Verifier / Compliance Checker / Compiler.
5. Do not use gradient fills, glassmorphism, or drop shadows heavier than elevation 8 — Fluent 2 is flat with subtle depth.

## PROMPT (end ⬆)

---

## How to iterate after the first Figma Make pass

- **Refine one screen**: `Refine screen L4 — Wizard · Modules. Make the module picker cards taller and add an icon per module. Move the template override control above the source location.`
- **Add variants**: `Create a dark-mode variant of screens L6 and U1 using Fluent 2 dark tokens (background #1F1F1F, text #FFFFFF, brand #2899F5).`
- **Convert to responsive**: `Produce a 1024 px width variant of L4 and U1 where the left rail collapses to icons only.`
- **Export**: use Figma Make's **"Export as Figma file"** to bring the frames into your Fluent 2 library and swap primitives for real library components.
