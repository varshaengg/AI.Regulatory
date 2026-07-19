---
name: kpi-impact-assessment
description: "KPI framework for quantifying business value, ROI, and improvement opportunities. Builds 6-8 metrics with baselines, quantifies 5+ improvements in $/hours/%, calculates ROI, writes a 300-word executive summary, and states the risk of inaction. USE FOR: KPI framework, impact assessment, ROI, business case metrics, improvement opportunities, executive summary, risk of inaction. DO NOT USE FOR: architecture design, compliance review, deployment, data exploration."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# KPI Framework for Impact Assessment

Quantify the business impact of a technology initiative, process change, or AI adoption effort. Produces a report with 6–8 KPIs, 5+ improvement opportunities (in $, hours, %), ROI, a 300-word executive summary, and risk of inaction.

Baselines must come from the user's provided materials. If a baseline is not available, mark it as **N/A** — do not fabricate or estimate baselines.

## When to Activate

- "build a KPI framework"
- "quantify the impact"
- "calculate ROI for this initiative"
- "impact assessment"
- "business case metrics"
- "what's the value of this project?"
- "improvement opportunities"
- "risk of inaction"
- "executive summary for leadership"
- "baseline metrics for this initiative"
- Preparing business justification for a new tool, platform, or process
- Quantifying AI/automation adoption benefits
- Comparing before/after states for a transformation initiative

## When NOT to Use

- Architecture design or HLD workflows (use `architecture-design` skill)
- Compliance or security reviews (use `compliance-enforcement` skill)
- Deployment execution (use `microsoft-engineering` skill)
- Exploring raw data files (use `data-explorer` skill)
- Writing code to language standards (use language instructions)

## Prerequisites

| Requirement          | Purpose                         | Notes                                                    |
| -------------------- | ------------------------------- | -------------------------------------------------------- |
| **Source Materials** | Baseline data for KPI grounding | Project docs, telemetry, surveys, ADO data, past reports |
| **Initiative Scope** | What is being assessed          | Clear description of the change, tool, or process        |
| **Audience**         | Determines summary depth        | Executive, engineering lead, PM, or mixed                |

## Workflow

### 1. Discovery

Ask the user before building anything:

1. **What** is being assessed?
2. **Scope** — which teams, systems, or processes?
3. **Current state** — pain points, manual steps, inefficiencies?
4. **Desired state** — what does success look like in 6–12 months?
5. **Available data** — existing metrics, dashboards, ADO boards, incident logs?
6. **Audience** — who reads the report? (VP, Director, PM, Eng Lead)
7. **Time horizon** — ROI period? (default: 12 months)

All answers optional — more detail produces a more grounded assessment. After discovery, scan any provided materials or the codebase for quantitative signals (commit frequency, build times, incident counts, team size).

### 2. KPI Framework (6–8 Metrics)

Select from the [KPI Catalog](references/kpi-catalog.md). Each KPI needs: **Name, Category** (Velocity / Quality / Cost / Risk / Experience / Compliance), **Baseline, Target, Δ%, Measurement Method, Data Source, Cadence**.

Rules:

- At least one KPI each from Velocity, Quality, and Cost
- At least one Risk or Compliance KPI
- Every KPI must be quantifiable
- Baselines must come from provided materials; if unavailable, mark as **N/A**

Output as a table — see the [report template](references/templates/impact-report.md) for format.

### 3. Improvement Opportunities (5+)

Quantify each opportunity in three dimensions:

- **Dollars** — annual cost savings or revenue impact
- **Hours** — time saved per year
- **Percentage** — relative improvement from baseline

Methodology: `hours/week × people × loaded_rate × improvement_% × 52` — see [ROI Methodology](references/roi-methodology.md) for loaded cost rates and calculation patterns. Every number must cite a source or state an assumption.

### 4. ROI Calculation

Structure: Investment Costs (licensing, implementation, training, ongoing) vs. Projected Returns (cost savings, productivity gains, risk reduction, quality improvements).

Rules:

- Conservative estimates — use the low end of ranges
- Apply **20% confidence discount** to all projected returns
- Calculate: **ROI %**, **payback period** (months), **3-year NPV** (8% discount rate)
- See [ROI Methodology](references/roi-methodology.md) for formulas and sensitivity analysis

### 5. Executive Summary (300 Words)

Follow the [Executive Summary Template](references/templates/executive-summary.md):

1. Open with the business problem (1 sentence)
2. State the proposed action (2–3 sentences)
3. Present top 3 KPI impacts with before/after numbers
4. Headline ROI: investment, return, ROI %, payback
5. Quantify total $ savings and hours freed
6. Close with risk of inaction
7. End with a call to action

Format: **bold** all numbers, short sentences (≤25 words), one comparison analogy, no jargon.

### 6. Risk of Inaction

Frame what happens if the organization does nothing across four dimensions:

- **Financial** — costs that continue or compound
- **Competitive** — advantage lost to peers
- **Operational** — systems that degrade or break
- **Talent** — retention and satisfaction impact

State total cost of inaction at 12 and 36 months.

### 7. Assemble Report

Save the full deliverable to `.copilot-tracking/impact-assessments/{{initiative-slug}}-impact-assessment.md` using the [Impact Report Template](references/templates/impact-report.md).

## Validation Checklist

- [ ] 6–8 KPIs with baselines, targets, and methods (covers Velocity, Quality, Cost, + Risk/Compliance)
- [ ] 5+ improvement opportunities quantified in $, hours, and %
- [ ] ROI with investment costs, returns, confidence discount, payback, and 3-year NPV
- [ ] 270–330 word executive summary
- [ ] Risk of inaction across Financial, Competitive, Operational, and Talent
- [ ] Every number cites a source or declares an assumption

## Standards Alignment

Grounded in **Azure Well-Architected Framework** (Cost Optimization, Operational Excellence), **DORA Metrics**, **Microsoft S360 KPIs**, **Forrester TEI Methodology**, and the **OKR Framework**.
