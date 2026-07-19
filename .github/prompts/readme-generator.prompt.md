---
description: 'Enterprise-grade README.md generator that analyzes the entire workspace and existing copilot-instructions.md to produce a human-focused project handbook — complementary to (not duplicating) the AI-focused copilot-instructions.md.'
---

# README.md Generator for Microsoft Enterprise Repositories

## Role

You are a **senior software engineer** with extensive experience in open-source and enterprise projects. You create appealing, informative, and easy-to-read README files that get developers productive quickly.

## Purpose & Philosophy

This prompt generates a **human-focused README.md** — the Project Handbook — by analyzing the full workspace.

### Core Principle: Written for Humans, Always

> **The README exists for one audience only: humans — developers, PMs, testers, and anyone new to the repo.**
>
> Every sentence, heading, diagram, and link must be written with this audience in mind. If a section would only make sense to an AI agent or a CI pipeline, it does not belong in the README. When in doubt, ask: *"Would a human reading this at 9 AM on their first day find it helpful?"* If the answer is no, cut it or rephrase it.

This principle overrides all other formatting and structural guidelines. Clarity and approachability for humans always come first.

### Key Distinction (README vs Copilot Instructions)

| Dimension          | README.md (this output)                 | copilot-instructions.md              |
|--------------------|------------------------------------------|--------------------------------------|
| **Audience**       | Humans (developers, reviewers, new joiners) | AI (Copilot, agents)              |
| **Purpose**        | Explain the project                      | Control AI behavior                  |
| **Tone**           | Narrative, descriptive, welcoming        | Strict, concise, enforceable rules   |
| **Focus**          | What, why, how-to-use, onboarding        | Constraints, patterns, do/don't      |
| **Applied**        | Read manually by people                  | Injected automatically into Copilot  |
| **Risk if missing**| Humans get confused                      | AI generates wrong code              |

**Anti-pattern to avoid:** Copying rules and constraints from copilot-instructions.md into the README. The README *explains*; copilot-instructions *constrains*. They are complementary, not redundant.

---

## Configuration Variables

- `DETAIL_LEVEL`: Standard|Comprehensive|Minimal <!-- How detailed the README should be -->
- `AUDIENCE`: Internal Microsoft|Mixed (Internal + External)|Open Source <!-- Who will read this README -->
- `INCLUDE_BADGES`: Yes|No <!-- Whether to include CI/CD, coverage, and status badges -->
- `INCLUDE_ARCHITECTURE_DIAGRAM`: Yes|No <!-- Whether to include a Mermaid architecture diagram -->
- `INCLUDE_TROUBLESHOOTING`: Yes|No <!-- Whether to include a troubleshooting section -->
- `INCLUDE_FAQ`: Yes|No <!-- Whether to include a FAQ section -->

---

## Analysis Protocol (MANDATORY — Execute Before Generating)

Before writing a single line of README, perform the following workspace analysis:

### Step 1: Discover Repository Identity

```
Scan for project identity signals:
- Root files: README, README.md, DESCRIPTION, package.json (name/description), *.csproj (RootNamespace), *.sln
- CI/CD files: azure-pipelines.yml, .github/workflows/*.yml — extract project name, build targets, environments
- Metadata: es-metadata.yml, owners.txt, LICENSE, CODEOWNERS
- Documentation: /documentation/**, /docs/**, /TSG/**
```

### Step 2: Discover Technology Stack

```
Detect all languages, frameworks, and tools:
- .NET: *.sln, *.csproj → extract TargetFramework, PackageReferences, project names
- Node.js/TypeScript: package.json → extract dependencies, scripts, engines
- Python: requirements.txt, pyproject.toml, setup.py
- SQL: *.sql files, Database/, Synapse/ directories
- Azure: host.json, function.json, azure-pipelines.yml, ARM/Bicep templates
- Data Pipelines: AzureDataFactoryTemplate/, pipeline/, dataflow/
- Power Platform: PCFControls/, solution.xml, *.pcfproj
- Containers: Dockerfile, docker-compose.yml, K8/ (Kubernetes manifests)
- Analytics: PowerBi/, Notebooks/ (Jupyter, Scala)
```

### Step 3: Map Repository Structure

```
Walk the directory tree and classify each top-level folder:
- Source code directories and their purpose
- Infrastructure and deployment directories
- Documentation and guide directories
- Test directories and their frameworks
- Script and tooling directories
- Configuration and template directories
```

### Step 4: Extract Existing Context from copilot-instructions.md

```
If .github/copilot-instructions.md exists:
- Extract: project description, business domain, tech stack summary, architecture overview
- Extract: repo structure description (reuse this — it's factual, not a rule)
- DO NOT copy: coding rules, "do/don't" constraints, AI behavior directives, quality gates
- Use the factual information as a foundation; rewrite it in a human-friendly narrative tone
```

### Step 5: Detect Prerequisites & Setup Requirements

```
Identify setup requirements by scanning:
- SDK/runtime versions from project files (TargetFramework, engines, python_requires)
- Package managers: NuGet.config, package-lock.json, yarn.lock, pip
- Environment variables: .env.example, launchSettings.json, local.settings.json
- Infrastructure dependencies: Docker, Kubernetes, Azure subscriptions
- Tool requirements: Azure CLI, Power Platform CLI, Azure Functions Core Tools, paconn
- Database setup: migration scripts, seed data, connection strings
```

### Step 6: Detect Build, Test, and Run Commands

```
Identify developer workflow commands from:
- CI/CD pipelines: azure-pipelines.yml, GitHub Actions
- Package scripts: package.json scripts, Makefile, build.ps1, build.sh
- .NET: dotnet build, dotnet test, dotnet run commands per project
- Documented scripts: /Scripts/ directory
- Test frameworks: xUnit, Jest, pytest — and how to invoke them
```

### Step 7: Identify Documentation & Support Resources

```
Find existing docs and support materials:
- /documentation/ folder contents (docfx, markdown files)
- /TSG/ folder (Troubleshooting Guides)
- Architecture Decision Records (ADRs)
- Wiki links, internal portal links
- Team contacts (owners.txt, CODEOWNERS)
```

### Step 8: Read & Absorb Existing Documentation (MANDATORY)

```
Before generating the README, thoroughly read ALL existing documentation in the repository:
- Read every .md file under /documentation/, /docs/, /TSG/, and any other doc-like folders
- Read any existing README.md files in subdirectories for component-level context
- Read architecture docs, design docs, onboarding guides, and runbooks
- Note what topics are ALREADY well-documented elsewhere in the repo
- Build a mental map of: "What exists? Where does it live? How do I link to it?"

Purpose: The README must NEVER duplicate content that already exists in the repo.
Instead, it must reference and link to existing docs. This step ensures the README
acts as a navigational hub pointing readers to the right place — not a copy of
what's already written.
```

---

## README.md Output Structure

Generate the README.md using the following sections. Adapt depth based on `DETAIL_LEVEL`.

### MANDATORY: Audience Labels on Targeted Sections

> **Sections that target a specific audience MUST open with a one-line audience callout. This is non-negotiable for targeted sections — missing labels will cause readers to waste time on irrelevant content.**
>
> Repositories vary widely — some serve only developers, others include PMs, testers, architects, DevOps, or business stakeholders. Without an explicit audience label, readers waste time reading sections irrelevant to them, or worse, skip sections they actually need.
>
> **Format:** Place a blockquote line immediately after the section heading:
> ```
> ## Section Title
>
> > _📌 **For [audience].** [Optional one-sentence scope note.]_
> ```
>
> **Rules:**
> 1. **MANDATORY: Every section that targets a specific subset of readers MUST have an audience label** (e.g., developers, DevOps, PMs). No exceptions — if a section is not for everyone, it gets a label. Omitting it risks the section being overlooked by the people who need it most.
> 2. **Do NOT add an audience label to universally relevant sections** — Overview, License, FAQ, Key Business Concepts, and similar sections that every reader would naturally read. Labeling these "For everyone" adds visual clutter without helping anyone. If a section is obviously for everyone, the absence of a label already signals that.
> 3. Use the most specific audience descriptor that fits (e.g., "developers & DevOps" is better than just "technical readers").
> 4. If a section has no clear audience, that is a signal the section may not belong in the README — reconsider whether it adds value.
>
> **Common audience labels** (use these or combine them as needed):
> | Label | Typical Sections |
> |---|---|
> | _(no label needed)_ | Overview, Architecture (high-level), Key Business Concepts, FAQ, License |
> | **For developers** | Repository Structure, Technology Stack, Contributing |
> | **For developers & testers** | Getting Started, Build & Test, Development Workflow |
> | **For developers & DevOps** | Configuration, CI/CD, Kubernetes, Troubleshooting |
> | **For developers & architects** | Data Flow & Integration Points, Detailed Architecture |
> | **For PMs & stakeholders** | Business context sections, Roadmap |
> | **For new joiners** | Onboarding, Team & Support, Documentation & Resources |
>
> **Why this matters:** In enterprise repos consumed by cross-functional teams, unlabeled sections create friction. A PM shouldn't have to read through Kubernetes manifests to find business context. A new developer shouldn't wonder whether the Architecture section has the detail they need or is executive-level only. The label takes one line and saves every reader time — but only when it carries real information.

### Section 1: Title & Badges

```markdown
# {Human-Friendly Project Name}

<!-- The title MUST be human-readable and welcoming — NOT a raw repo slug or code identifier.
     ✅ Good: "Account Planning Apps — Customer Success Management"
     ✅ Good: "CSM Planning & Sales Play Platform"
     ❌ Bad:  "CSM-MSXMP-Plan-ACCTPL-Apps"
     ❌ Bad:  "Microsoft.ComSME.Sales.AccountPlan"
     Derive a clear, descriptive project name from the repo's business purpose. -->

<!-- Badges: build status, coverage, environment, version — if INCLUDE_BADGES == Yes -->

> One-paragraph elevator pitch: What this project is, who it serves, and why it exists.
> Written in plain language for a human who just opened this repo for the first time.
> Avoid jargon, internal acronyms, or code identifiers — explain like you're talking to a smart colleague.
```

### Section 2: Table of Contents

```markdown
## Table of Contents
<!-- Auto-generated TOC linking to all major sections below -->
<!-- Use proper heading hierarchy (h2 → h3 → h4) to enable GitHub's auto-generated table of contents -->
```

### Section 3: Overview

```markdown
## Overview

> _📌 **For everyone.** High-level context about the project — what it does, who it serves, and why it exists._

- **What** the system does (business purpose, not implementation details)
- **Who** it serves (target users, teams, customers)
- **Why** it exists (business problem it solves)
- **Where** it fits in the broader ecosystem (upstream/downstream systems)
```

### Section 4: Architecture

```markdown
## Architecture

### High-Level Architecture

<!-- MANDATORY: When INCLUDE_ARCHITECTURE_DIAGRAM == Yes, generate a Mermaid diagram using the
     :::mermaid / ::: fence syntax (NOT triple-backtick ```mermaid```) for Azure DevOps compatibility.
     Do NOT use ASCII art, PNG/SVG image links, or any other diagram format — Mermaid only.
     The diagram must be derived from components actually discovered in the workspace.
     Use the appropriate Mermaid diagram type based on what best represents the architecture:
       - `graph TB` or `graph LR` for component/flow diagrams
       - `flowchart TB` for data flow and pipeline diagrams
       - `C4Context` or `C4Container` for C4-model system context / container views (if the architecture is complex)

     RENDERING COMPATIBILITY (MANDATORY):
     - Use `:::mermaid` to open and `:::` to close the diagram block. This renders in Azure DevOps, GitHub, and VS Code.
     - Do NOT use `%%{init}%%` directives or `---config...---` YAML frontmatter inside the mermaid block.
       Azure DevOps bundles Mermaid ~v8–v9 which does not support these directives and will fail to render.
     - Do NOT specify a layout engine (e.g., `elk`). Use the default dagre layout only.

     DIAGRAM STYLING RULES (MANDATORY):
     1. **Adaptive & clean layout**: Use `graph LR` (left-to-right) for wide architectures, `graph TB` (top-down)
        for layered architectures. Choose the direction that minimizes crossing lines and feels natural to read.
     2. **Color-coded subgraphs**: Use `style` directives or `classDef` to assign distinct, professional colors
        to each layer/subgraph so readers can instantly distinguish components at a glance.
        Recommended palette (adjust for the number of layers):
          - Client / UI layer:      fill:#E3F2FD,stroke:#1565C0,color:#000   (soft blue)
          - Application / API layer: fill:#E8F5E9,stroke:#2E7D32,color:#000   (soft green)
          - Data layer:             fill:#FFF3E0,stroke:#E65100,color:#000   (soft orange)
          - Integration layer:      fill:#F3E5F5,stroke:#6A1B9A,color:#000   (soft purple)
          - External / 3rd-party:   fill:#ECEFF1,stroke:#546E7A,color:#000   (neutral gray)
     3. **Neatness over completeness**: Show only the major components (5–12 nodes). If more detail is needed,
        add a second "zoomed-in" diagram for a specific subsystem. Never cram 20+ nodes into one diagram.
     4. **Readable labels**: Use short, human-friendly labels with `<br/>` line breaks for subtitles.
        ✅ "Planning API<br/>.NET 8.0"   ❌ "Microsoft.ComSME.Sales.PlanningAppsApi.WebApi"
     5. **No orphan nodes**: Every node must connect to at least one other node.

     OVERLAP REDUCTION TECHNIQUES (MANDATORY — dagre layout):
     These rules prevent edges from crossing over nodes or overlapping each other:
     6. **Subgraph declaration order controls vertical rank**: The first subgraph declared appears at the top.
        Declare subgraphs in the order that matches the natural flow (e.g., Infra → Client → App → Data → Analytics).
     7. **Node declaration order controls horizontal position**: Within a subgraph, the first node declared
        appears leftmost. Order nodes left-to-right to match where their outbound edges point.
     8. **Group edges by source node**: List all edges from one source together (e.g., all `API -->` edges
        in a block) before moving to the next source. This reduces crossings significantly.
     9. **Avoid cross-layer edges going upward**: If `AKS` is in Infra (top), declare Infra first so
        edges from AKS flow downward, not upward against the natural TB direction.
     10. **Use %% comment separators** between edge groups for readability (e.g., `%% Client to App`).

     Example (adapt to actual discovered components — note fence syntax, styling, and edge grouping):
-->

:::mermaid
graph TB
    subgraph Infra["Infrastructure"]
        style Infra fill:#ECEFF1,stroke:#546E7A,color:#000
        AKS["AKS Cluster"]
    end

    subgraph Client["Client Layer"]
        style Client fill:#E3F2FD,stroke:#1565C0,color:#000
        PCF["PCF Controls"]
        CRM["Dynamics 365"]
    end

    subgraph App["Application Layer"]
        style App fill:#E8F5E9,stroke:#2E7D32,color:#000
        API["Planning Apps API<br/>.NET 8.0"]
        FUNC["Azure Functions<br/>.NET 8.0, v4"]
    end

    subgraph Data["Data Layer"]
        style Data fill:#FFF3E0,stroke:#E65100,color:#000
        SQL[("SQL Server")]
        SYNAPSE[("Synapse Analytics")]
    end

    subgraph Integration["Integration Layer"]
        style Integration fill:#F3E5F5,stroke:#6A1B9A,color:#000
        ADF["Data Factory"]
        PBI["Power BI"]
    end

    %% Infra to App — flows downward naturally
    AKS --> API

    %% Client to App
    PCF --> API
    CRM --> FUNC

    %% App to Data — ordered to match node positions
    API --> SQL
    FUNC --> SQL
    FUNC --> SYNAPSE

    %% Integration layer
    ADF --> SQL
    ADF --> SYNAPSE
    SYNAPSE --> PBI
:::

<!-- Follow the Mermaid diagram with a brief narrative description of the main components and how they interact -->

### Key Components
<!-- Table or list of major components with one-line descriptions -->
<!-- Example:
| Component               | Purpose                                    | Technology        |
|-------------------------|--------------------------------------------|-------------------|
| Planning Apps API       | RESTful APIs for account planning           | .NET 8.0 Web API  |
| Azure Functions         | Serverless event-driven processing          | .NET 8.0, v4      |
| PCF Controls            | Custom Power Platform UI components         | TypeScript, PCF   |
| Data Factory Pipelines  | ETL and data transformation                 | Azure Data Factory |
-->
```

### Section 5: Repository Structure

<!-- NOTE ON AUDIENCE: This section is for developers navigating the codebase.
     Add a brief callout so PMs know they can skip it. -->

```markdown
## Repository Structure

> _📌 **For developers.** This section maps the codebase layout and folder purposes._

<!-- Human-readable annotated directory tree -->
<!-- Focus on WHAT each folder contains and WHY a developer would go there -->
<!-- Example:
```
├── Code/
│   ├── FunctionApps/       # Azure Functions (serverless APIs & jobs)
│   ├── PlanningAppsApi/    # Main REST API service
│   ├── CRM/                # Power Platform / CRM integrations
│   │   └── PCFControls/    # 30+ custom Power Platform controls
│   ├── Common/             # Shared libraries and utilities
│   ├── Database/           # SQL schemas, migrations, stored procs
│   ├── Synapse/            # Analytics queries and pipelines
│   └── PowerBi/            # Business intelligence reports
├── Code - CRMPlugIn/       # Dynamics 365 server-side plugins
├── AzureDataFactoryTemplate/ # Data pipeline definitions (ADF)
├── documentation/          # Project docs (DocFX format)
├── TSG/                    # Troubleshooting guides (DRI runbooks)
└── Scripts/                # Build and utility scripts
```
-->
```

### Section 6: Technology Stack

<!-- NOTE ON AUDIENCE: This section is a detailed version matrix for developers.
     PMs get the high-level view from Architecture. -->

```markdown
## Technology Stack

> _📌 **For developers.** Detailed version matrix — see [Architecture](#architecture) for the high-level view._

<!-- Organized table of technologies with versions detected from the workspace -->
<!-- Example:
| Layer           | Technology                  | Version   |
|-----------------|-----------------------------|-----------|
| Backend         | .NET / C#                   | 8.0 (LTS) |
| Serverless      | Azure Functions Runtime     | v4        |
| Frontend / PCF  | TypeScript                  | ^5.0      |
| ORM             | Entity Framework Core       | ^8.0      |
| Database        | SQL Server / Azure Synapse  | —         |
| Data Pipelines  | Azure Data Factory          | v2        |
| Analytics       | Power BI, Jupyter Notebooks | —         |
| CRM             | Dynamics 365 / Dataverse    | —         |
| Containers      | Kubernetes (AKS)            | —         |
-->
```

### Section 7: Getting Started

<!-- NOTE ON AUDIENCE: This section is primarily for developers and testers.
     Add a brief callout at the top so PMs and non-technical readers know they can skip it:
     > _"📌 This section covers local development setup. If you're a PM or non-engineering stakeholder,
     > feel free to skip ahead to [Key Business Concepts](#key-business-concepts)."_
-->

```markdown
## Getting Started

> _📌 **For developers & testers.** If you're a PM or non-engineering stakeholder, you can skip ahead to [Key Business Concepts](#key-business-concepts)._

### Prerequisites
<!-- List ALL required tools, SDKs, accounts, and subscriptions -->
<!-- Include exact version requirements and installation links -->

### Clone & Setup
<!-- Step-by-step: clone, restore dependencies, configure environment -->

### Build
<!-- Exact commands to build each major component -->

### Run Locally
<!-- How to run the application locally (or specific components) -->
<!-- Include any required environment variables or config files -->

### Usage Examples
<!-- Include relevant code snippets or usage examples that show how to interact with the project -->
<!-- Focus on the most common developer tasks to get users productive quickly -->

### Run Tests
<!-- Exact commands to run the test suites -->
<!-- Include coverage expectations if relevant -->
```

### Section 8: Development Workflow

<!-- NOTE ON AUDIENCE: This section is primarily for developers.
     Add the same skip-ahead callout for PMs. -->

```markdown
## Development Workflow

> _📌 **For developers.** PMs and non-engineering stakeholders can skip to [Key Business Concepts](#key-business-concepts)._

### Branching Strategy
<!-- Describe the branching model: trunk-based, GitFlow, etc. -->

### Pull Request Process
<!-- Steps for submitting a PR, review expectations, CI checks -->

### CI/CD Pipeline
<!-- Brief description of what the pipeline does -->
<!-- Reference azure-pipelines.yml or GitHub Actions -->

### Environments
<!-- List deployment environments: Dev, Staging, Prod — and how to target them -->
```

### Section 9: Key Business Concepts (Domain Context)

```markdown
## Key Business Concepts

> _📌 **For everyone.** Domain terminology and concepts that help new joiners understand the business context._

<!-- Explain the domain in plain language so new joiners understand the "what" and "why" -->
<!-- Example for this repo:
- **Account Plan**: A strategic plan for managing and growing a customer account
- **Sales Play**: A structured sales motion targeting a specific market opportunity
- **Actionable Recommendation**: A data-driven suggestion for a seller to act on
- **CPE (Customer Planning Experience)**: The UI and workflows for planning activities
-->
```

### Section 10: Data Flow & Integration Points

<!-- NOTE ON AUDIENCE: This section is for developers and architects understanding system integration. -->

```markdown
## Data Flow & Integration Points

> _📌 **For developers & architects.** Describes how data moves between services and external systems._

<!-- Describe how data moves through the system — in human-readable narrative or diagram -->
<!-- Mention external systems: CRM Dynamics, Azure Data Factory, Synapse, Power BI -->
<!-- Example:
1. Source systems feed data into **Azure Data Factory** pipelines
2. ADF transforms and stages data into **SQL Server / Synapse**
3. **Planning Apps API** serves data to the frontend and CRM
4. **Power BI** reports consume the data warehouse for analytics
5. **PCF Controls** in Dynamics 365 display actionable insights to sellers
-->
```

### Section 11: Troubleshooting

<!-- NOTE ON AUDIENCE: This section is for developers and DevOps resolving common issues. -->

```markdown
## Troubleshooting

> _📌 **For developers & DevOps.** Common issues and quick fixes for local development and deployment._

<!-- Include if INCLUDE_TROUBLESHOOTING == Yes -->
<!-- Link to /TSG/ folder for DRI runbooks -->
<!-- List common issues and quick fixes -->
<!-- Example:
| Issue                          | Quick Fix                                      |
|--------------------------------|------------------------------------------------|
| Azure Function won't start     | Check `local.settings.json` and Key Vault access |
| PCF control build fails        | Run `npm install` and verify Node.js version    |
| Database migration error       | Ensure connection string and run `dotnet ef ...` |
-->
```

### Section 12: FAQ

```markdown
## FAQ

> _📌 **For everyone.** Answers to the most common questions new contributors and stakeholders ask._

<!-- Include if INCLUDE_FAQ == Yes -->
<!-- Answer the top 5-10 questions a new developer would ask -->
```

### Section 13: Documentation & Resources

```markdown
## Documentation & Resources

> _📌 **For new joiners.** Links to deeper documentation, runbooks, wikis, and related references._

<!-- Links to:
- /documentation/ (DocFX site)
- /TSG/ (Troubleshooting Guides)
- Architecture Decision Records
- Internal wikis or portals
- Related repositories
- Microsoft Learn references
-->
```

### Section 14: Team & Support

```markdown
## Team & Support

> _📌 **For new joiners.** Who owns this project, how to get help, and team contact channels._

<!-- Team name, ownership, DRI rotation -->
<!-- Contact channels: Teams, email alias, ICM -->
<!-- Reference owners.txt or CODEOWNERS -->
```

### Section 15: Contributing

<!-- NOTE ON AUDIENCE: This section is for developers submitting code changes. -->

```markdown
## Contributing

> _📌 **For developers.** How to submit changes, coding standards, and PR process._

<!-- How to contribute: fork/branch, coding standards reference, PR requirements -->
<!-- Link to copilot-instructions.md for AI-assisted development standards -->
<!-- Link to CONTRIBUTING.md if it exists -->
```

### Section 16: License

```markdown
## License

> _📌 **For everyone.**_

<!-- License information or "Microsoft Internal — Confidential" for internal repos -->
```

---

## Authoritative README Guidelines (Reference Material)

When generating the README, follow best practices from these authoritative sources:

### Internal Project READMEs — InCycle Software Guidelines
**Source:** https://blogs.incyclesoftware.com/readme-files-for-internal-projects

Key principles to apply:
- **Internal READMEs matter just as much as open-source ones** — treat them as living onboarding documents, not afterthoughts
- **Answer the "5-minute questions"**: What does this do? How do I build it? How do I run it? How do I deploy it? Who do I ask for help?
- **Include environment setup details** that are often assumed but never documented (VPN, internal NuGet feeds, Azure subscriptions, access permissions)
- **Document tribal knowledge** — the things everyone "just knows" but new joiners don't (internal tool URLs, team conventions, deployment schedules)
- **Keep it maintainable** — a README that's 50% outdated is worse than no README; focus on stable information and link to volatile docs elsewhere
- **Link to runbooks and TSGs** rather than duplicating operational procedures inline

### Azure DevOps README Best Practices — Microsoft Learn
**Source:** https://learn.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops

Key principles to apply:
- **Place README.md at the root** of the repository so it renders automatically in Azure DevOps and GitHub
- **Use Markdown formatting** — headers, tables, code blocks, and links for scannability
- **Start with a clear project name and description** — the first paragraph should tell a reader what this repo is in ≤ 3 sentences
- **Include build and test instructions** with exact commands — don't assume the reader knows the toolchain
- **Document prerequisites explicitly**: SDKs, tools, access requirements, and environment configuration
- **Add a section on how to contribute** — branching strategy, PR process, review expectations
- **Use relative links** to reference other files in the repo (not absolute URLs that break across forks/clones)
- **Keep the README focused and layered** — provide a concise overview at the top, then progressively detailed sections below; link out to `/docs` or `/documentation` for deep dives

---

## What NOT to Include

Keep the README focused and avoid bloat. Do **not** include:

- **Detailed API documentation** — link to separate docs (e.g., `/documentation/` or a wiki) instead of inlining API references
- **Extensive troubleshooting guides** — link to `/TSG/` or a wiki; only include a brief quick-fix table in the README
- **Full license text** — reference a separate `LICENSE` file instead
- **Detailed contribution guidelines** — reference a separate `CONTRIBUTING.md` file; only include a brief contributing section in the README
- **AI behavior rules or coding constraints** — those belong in `copilot-instructions.md`, not the README
- **Content that already exists in other repo documentation** — if a topic is covered in `/documentation/`, `/TSG/`, or any other doc folder, **do not reproduce it in the README**. Instead, write a one-line summary and link to the existing document. The README is a **navigational hub**, not a copy of everything.

### The "Link, Don't Repeat" Rule

> **If documentation for a topic already exists somewhere in the repo, the README must link to it — never restate it.**
>
> Before writing any section, check whether existing docs already cover it (see Step 8 in the Analysis Protocol). If they do:
> 1. Write a **one-sentence summary** of what the linked doc covers
> 2. Provide a **relative Markdown link** to the file
> 3. Move on — do not elaborate further in the README
>
> This keeps the README lean, avoids stale duplicate content, and respects the work already done by the team.

---

## Writing Style Guidelines

Follow these principles to ensure the README is **human-optimized** (not AI-optimized):

1. **Human-first, always**: Every word is for a human reader. Write for developers, PMs, testers, and new joiners. If a sentence wouldn't help a person, remove it.
2. **Narrative tone**: Write as if explaining to a smart colleague on their first day. Use plain language — expand acronyms on first use, avoid code identifiers as headings, and explain domain terms.
3. **Show, don't tell**: Use concrete examples, commands, and screenshots over abstract descriptions
4. **Scannable**: Use tables, bullet points, and headers — avoid walls of text
5. **Actionable**: Every section should help someone DO something (build, run, debug, contribute)
6. **Current**: Reference actual file paths, commands, and versions discovered from the workspace
7. **Audience label on EVERY section (MANDATORY)**: Every section — without exception — must open with a one-line audience callout (`> _📌 **For [audience].**_`). This applies to developer-focused sections AND general sections alike:
   - _"📌 **For developers.**"_ — Repository Structure, Technology Stack, Project Components, Contributing
   - _"📌 **For developers & testers.**"_ — Getting Started, Build & Test, Development Workflow
   - _"📌 **For developers & DevOps.**"_ — Configuration, Kubernetes & Infrastructure, Troubleshooting, CI/CD
   - _"📌 **For developers & architects.**"_ — Data Flow & Integration Points
   - _"📌 **For everyone.**"_ — Overview, Architecture (high-level), Key Business Concepts, FAQ, License
   - _"📌 **For new joiners.**"_ — Team & Support, Documentation & Resources, Onboarding

   **No section may omit the audience label.** If you cannot identify who a section is for, reconsider whether it belongs in the README. See the full mandatory rule in the "MANDATORY: Audience Label on Every Section" block above for rationale and formatting details.
8. **No duplication with copilot-instructions.md**: Don't repeat AI rules; instead link to it:
   > _"For coding standards and AI-assisted development guidelines, see [`.github/copilot-instructions.md`](.github/copilot-instructions.md)."_
9. **Mermaid diagrams** over ASCII art for architecture visuals — styled with color-coded layers, clean layout, and human-readable labels
10. **Relative links** to files and folders within the repo (e.g., `docs/CONTRIBUTING.md`) — ensure links work when the repository is cloned
11. **Answer the 5-minute questions** (per InCycle guidelines): What? How to build? How to run? How to deploy? Who to contact?
12. **Document tribal knowledge** — internal tools, NuGet feeds, VPN requirements, Azure subscription access, team conventions
13. **Layer information progressively** (per Microsoft Learn): concise overview first, detailed sections below, deep dives linked out
14. **Link, don't repeat**: If content exists in `/documentation/`, `/TSG/`, or other repo docs, link to it — never duplicate it in the README

---

## Quality Checklist (Self-Validate Before Output)

Before producing the final README.md, verify:

- [ ] Every technology listed was **actually detected** in the workspace (no guessing)
- [ ] Every command listed is **actually runnable** based on project files
- [ ] Every file/folder referenced **actually exists** in the workspace
- [ ] No AI behavior rules or coding constraints were copied from copilot-instructions.md
- [ ] The README can stand alone — a new developer can onboard from it without other docs
- [ ] Architecture diagrams (if included) accurately reflect the discovered components
- [ ] Prerequisites are complete — nothing is assumed or left out
- [ ] **Every section** has an audience label (`📌 **For [audience].**`) — no section is left unlabeled
- [ ] The tone is human-friendly, not robotic or template-like
- [ ] Internal links use relative paths and are valid
- [ ] The README uses **GitHub Flavored Markdown** (GFM) throughout
- [ ] Heading hierarchy is clean (`##` → `###` → `####`) to enable GitHub's auto-generated table of contents
- [ ] The total README size is **under 500 KiB** (GitHub truncates rendering beyond this limit)
- [ ] Code examples and usage snippets are included where they help developers get started faster
- [ ] Detailed API docs, full license text, and extensive troubleshooting are linked out — not inlined

---

## Cleanup: Remove Legacy README Files

Before or after generating the new `README.md`, check for and remove legacy README files in the repository root that are **exactly named `README`** (with or without a non-`.md` extension).

**Scope — what to delete:**
- `README` (no extension) — **this file currently exists in the repo root** (an empty HTML stub) and **must** be deleted
- `README.txt`, `README.html`, `README.rst`, `README.pdf`, or any `README.<ext>` where `<ext>` is **not** `.md`
- `README.md` — if it already exists, **delete it** so the newly generated README starts from a clean slate

**Scope — what to preserve (do NOT delete):**
- Any other file that merely *contains* "README" in its name (e.g., `README-generator.md`, `about-readme.txt`, `difference-between-readme-and-copilot-instructions.md`) — these are **not** legacy README files and must not be touched

**Action required:**
1. Scan the repo root for files matching exactly `README`, `README.md`, or `README.<non-md extension>`
2. **Delete all of them** — including the existing `README.md`
3. **Create a brand-new `README.md`** with the freshly generated content
4. Confirm only a single `README.md` remains at the repository root and no unrelated files were removed

> **Why:** Azure DevOps and GitHub both render `README.md` by default. Having a stale extensionless `README` file alongside `README.md` creates ambiguity, confuses new joiners, and may display the wrong content in some Git hosting UIs.

---

## Expected Output

A single, complete, enterprise-quality **README.md** file that serves as the **Project Handbook** for human developers — complementary to the AI-focused copilot-instructions.md. The README explains *what the project is and how to work on it*; it does not dictate *how AI should write code*.

As part of the output workflow, **delete every README variant** in the repo root — `README` (no extension), `README.<non-md extension>`, and any existing `README.md` — then **create a brand-new `README.md`** with the freshly generated content. No other files containing "README" in their name are affected.
