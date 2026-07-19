---
name: presentation-builder
description: "Cross-cutting skill for generating Microsoft-branded PowerPoint decks from structured content. Consumes YAML slide definitions and a .potx template to produce editable .pptx files with native text, tables, and speaker notes. USE FOR: generate PowerPoint, create slide deck, build presentation, export to pptx, executive deck, stakeholder presentation, business case deck, status update deck, keynote slides. DO NOT USE FOR: visual design or branding decisions (agents own the narrative), graphic design or image generation, Figma-to-slide conversion, Google Slides output."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Presentation Builder — PowerPoint Generation Skill

Use this skill when any agent or recipe needs to produce an actual `.pptx` file from structured content. The skill takes a YAML slide plan and generates an editable, Microsoft-branded PowerPoint deck using `python-pptx`.

**This is a utility skill, not a narrative coach.** Agents like the Stakeholder Communicator own the content strategy, audience framing, and storytelling. This skill owns the file generation.

## When to Activate

- "generate a PowerPoint"
- "create a slide deck"
- "turn this into a presentation"
- "export to pptx"
- "build me a deck"
- "make this into slides"
- After a Presentation Outline artifact has been created
- When any agent produces structured slide content that needs to become a file

## When NOT to Use

- Creating the content strategy or narrative (agents handle this)
- Visual design decisions or branding guidelines (follow Microsoft template)
- Generating AI images for slides (out of scope — use image-generation skills)
- Producing Google Slides or Keynote output

## Prerequisites

| Tool            | Purpose                                        | Install Command                                   |
| --------------- | ---------------------------------------------- | ------------------------------------------------- |
| **Python 3.9+** | Runtime                                        | Pre-installed or `winget install Python.Python.3` |
| **python-pptx** | PowerPoint generation                          | `pip install python-pptx`                         |
| **PyYAML**      | YAML parsing                                   | `pip install pyyaml`                              |
| **Pillow**      | Image handling (optional, for embedded images) | `pip install Pillow`                              |

> **One-liner install**: `pip install python-pptx pyyaml Pillow`

## Microsoft Template

The skill uses a Microsoft-branded `.potx` template for consistent visual identity. Place the template at:

```
.github/skills/presentation-builder/templates/microsoft-default.potx
```

If no template is provided, the skill generates a clean deck with Microsoft-standard dimensions (16:9 widescreen) and applies default styling consistent with Microsoft presentation guidelines:

- **Font**: Segoe UI (titles: 28pt bold, body: 18pt regular)
- **Colors**: Microsoft brand palette — primary blue (#0078D4), dark text (#1A1A1A), light background (#FFFFFF)
- **Slide size**: 13.333" x 7.5" (standard 16:9)

### Template Resolution

The script resolves templates using this priority:

1. **CLI `--template` argument** — highest priority, used if the file exists
2. **YAML `template` field** — read from the slide plan file
3. **Built-in defaults** — Segoe UI, Microsoft blue, blank layouts

When resolving the YAML `template` field, the script searches in order:

- Absolute path or relative to current working directory
- Relative to the slide plan file directory
- With `.potx` / `.pptx` extension appended (if no extension given)
- In the skill's own `templates/` folder (e.g., `"microsoft-default"` resolves to `templates/microsoft-default.potx`)

This means agents can set `template: "microsoft-default"` in the YAML and the script will find it automatically — as long as the `.potx` file exists in the skill's `templates/` folder.

### Template Configuration (Sidecar YAML)

Each `.potx` template can have a **sidecar `.yaml` config file** with the same base name (e.g., `microsoft-default.yaml` next to `microsoft-default.potx`). This config defines which template layouts and placeholder indices to use for each slide type — **so the Python script never needs to be modified when swapping templates**.

```yaml
# Template configuration example
layouts:
  title: "2_Title_Gradient_Warm Gray"
  section: "Section_Gradient_Warm Gray"
  content: "1-column_Text"
  two-column: "2-column_Text_with Subheads"
  table: "2_Custom Layout"
  summary: "Summary page_Big headline"
  ask: "2-column_Text_with Subheads"
  blank: "Blank_with Head"

placeholders:
  title:
    title: 13
    subtitle: 12
  section:
    title: 0
    subtitle: 12
  content:
    title: 0
    body: 15
  two-column:
    title: 0
    left_heading: 16
    left_body: 15
    right_heading: 19
    right_body: 20
  # ... (see microsoft-default.yaml for the full example)
```

**To add a new template:**

1. Place the `.potx` file in the `templates/` folder
2. Run `dump_layouts.py` (in `test-ppt/`) to list available layout names and placeholder indices
3. Create a matching `.yaml` config mapping slide types to the correct layouts and placeholders
4. Set `template: "your-template-name"` in `slides.yaml`

If no sidecar config exists, the script falls back to standard PowerPoint placeholder conventions (0=Title, 1=Body).

## Supported Slide Types

| Type         | Layout           | Content                                              |
| ------------ | ---------------- | ---------------------------------------------------- |
| `title`      | Title slide      | Title, subtitle, presenter, date                     |
| `section`    | Section divider  | Section heading, optional subtitle                   |
| `content`    | Title + bullets  | Slide title, bullet points (up to 6), speaker notes  |
| `two-column` | Side-by-side     | Left column, right column, each with title + bullets |
| `table`      | Title + table    | Slide title, table data (headers + rows)             |
| `summary`    | Key takeaway     | Headline metric or statement, supporting points      |
| `ask`        | Decision slide   | The ask, supporting rationale, next steps            |
| `blank`      | Empty with title | Title only — for manually adding visuals later       |

## YAML Slide Plan Format

Create a YAML file with the presentation structure. Each agent that calls this skill is responsible for producing this YAML from its own artifacts.

```yaml
title: "Presentation Title"
subtitle: "Optional subtitle or tagline"
presenter: "Presenter Name"
date: "2026-04-09"
template: "microsoft-default" # optional — template name or path
aspect_ratio: "16:9" # 16:9 or 4:3

slides:
  - type: title
    title: "Main Title"
    subtitle: "Subtitle or value proposition"
    presenter: "Name"
    date: "2026-04-09"

  - type: section
    title: "Section Heading"

  - type: content
    title: "Slide Title"
    bullets:
      - "First point — keep it scannable"
      - "Second point — one idea per bullet"
      - "Third point — executives read bullets, not paragraphs"
    speaker_notes: |
      Talking point: Expand on the first bullet here.
      Mention the data from the insight report.

  - type: two-column
    title: "Before vs. After"
    left:
      heading: "Today"
      bullets:
        - "Manual process"
        - "4 hours per week"
    right:
      heading: "Proposed"
      bullets:
        - "Automated workflow"
        - "15 minutes per week"
    speaker_notes: "Emphasize the 93% time reduction."

  - type: table
    title: "ROI Summary"
    table:
      headers: ["Metric", "Value"]
      rows:
        - ["Investment", "$150K"]
        - ["Annual Value", "$180K – $320K"]
        - ["Payback Period", "6 – 10 months"]
        - ["Year 1 ROI", "20% – 113%"]
    speaker_notes: "Focus on the payback period — leadership cares about time-to-value."

  - type: summary
    title: "The Bottom Line"
    headline: "$150K–$290K net annual value"
    bullets:
      - "Validated with 3 pilot teams"
      - "Payback within first year"
      - "Low adoption risk with phased rollout"

  - type: ask
    title: "The Ask"
    ask: "Approve $150K investment and dedicate 2 engineers for Q3"
    rationale:
      - "ROI of 20–113% in Year 1"
      - "3 teams already requesting access"
    next_steps:
      - "Engineering kickoff — Week 1"
      - "MVP launch — Week 8"
      - "Full rollout — Week 14"
```

## Workflow

### Step 1: Prepare Content

The calling agent creates the YAML slide plan from its artifacts. Examples:

| Agent                    | Source Artifact           | Output Location                                             |
| ------------------------ | ------------------------- | ----------------------------------------------------------- |
| Stakeholder Communicator | `presentation-outline.md` | `.copilot-tracking/stakeholder-comms/{feature}/slides.yaml` |
| Business Case Builder    | `business-case.md`        | `.copilot-tracking/stakeholder-comms/{feature}/slides.yaml` |
| Any recipe               | structured content        | `.copilot-tracking/{context}/slides.yaml`                   |

### Step 2: Generate the Deck

**Option A — Direct execution** (when the terminal can write to the filesystem):

```bash
python .github/skills/presentation-builder/scripts/generate_pptx.py \
  --plan-file .copilot-tracking/stakeholder-comms/{feature}/slides.yaml \
  --output-file .copilot-tracking/stakeholder-comms/{feature}/presentation.pptx
```

With a custom template:

```bash
python .github/skills/presentation-builder/scripts/generate_pptx.py \
  --plan-file .copilot-tracking/stakeholder-comms/{feature}/slides.yaml \
  --output-file .copilot-tracking/stakeholder-comms/{feature}/presentation.pptx \
  --template .github/skills/presentation-builder/templates/microsoft-default.potx
```

**Option B — Standalone copy** (when the terminal is sandboxed or the skill script is not in the repo):

1. Copy `generate_pptx.py` into the **same directory** as `slides.yaml`
2. Tell the user to run it:

```bash
cd .copilot-tracking/stakeholder-comms/{feature}
pip install python-pptx pyyaml
python generate_pptx.py
```

The script auto-discovers `slides.yaml` in the same directory and writes `presentation.pptx` next to it. No arguments needed.

> **Sandboxed terminal detection**: If the agent's terminal cannot write files (common in VS Code Copilot agent mode), always use Option B. Copy the script next to `slides.yaml` and instruct the user to run the commands from their own terminal.

**Parameters:**

| Flag            | Required | Default                                               | Description                              |
| --------------- | -------- | ----------------------------------------------------- | ---------------------------------------- |
| `--plan-file`   | No       | Auto-discovers `slides.yaml` next to script or in cwd | Path to YAML slide plan                  |
| `--output-file` | No       | `presentation.pptx` next to the plan file             | Output path for the `.pptx` file         |
| `--template`    | No       | Built-in Microsoft defaults                           | Path to `.potx` or `.pptx` template file |

### Step 3: Confirm and Iterate

After generation, the calling agent should:

1. Confirm the file was created: _"Your presentation is ready at `.copilot-tracking/stakeholder-comms/{feature}/presentation.pptx`."_
2. Remind the user it is fully editable: _"The deck uses native text and tables — you can open it in PowerPoint and adjust anything."_
3. Suggest next steps: _"Want me to adjust the content, add more slides, or create talking points to go with it?"_

## Integration Pattern

Agents reference this skill the same way they reference `pii-scrubbing` or `business-friendly-language` — as a cross-cutting capability they invoke when needed.

**In agent files**, add a reference:

```markdown
When the user wants an actual PowerPoint file, invoke the `presentation-builder` skill
in `.github/skills/presentation-builder/SKILL.md` to generate the deck from the
structured slide content.
```

**In recipes**, add a step:

```markdown
### Step N: Generate Presentation (Optional)

If the user wants a PowerPoint deck, invoke the `presentation-builder` skill:

1. Convert the presentation outline to YAML slide plan format
2. Run the generation script
3. Confirm the output file with the user
```

## File Organization

```
.github/skills/presentation-builder/
├── SKILL.md                              # This file
├── templates/
│   ├── microsoft-default.potx            # Microsoft-branded template
│   └── microsoft-default.yaml            # Layout + placeholder config for the template
└── scripts/
    └── generate_pptx.py                  # Python generation script
```

## Limitations

- **No animations or transitions** — `python-pptx` does not support creating animations. Add them manually in PowerPoint after generation.
- **No embedded charts** — for data visualizations, generate chart images separately and add them to slides manually. Tables are fully supported.
- **Template required for full branding** — without a `.potx` template, the skill applies sensible defaults but may not match your organization's exact brand guidelines.
- **SVG images not supported** — convert to PNG before embedding.
