---
name: data-explorer
description: "Business-friendly data exploration skill for Excel files. Reads spreadsheets, loads data into local SQLite, enables natural language queries, and generates charts and summaries. USE FOR: upload Excel, analyze spreadsheet, explore data, natural language query, build chart from data, data mining, pivot table, summarize CSV, data visualization, business intelligence, ad-hoc reporting, quick analysis, what does this data show, graph this data, query my spreadsheet, data insights. DO NOT USE FOR: production database design (use architecture-design skill), large-scale ETL pipelines, real-time dashboards, cloud database provisioning (use azure-prepare)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Data Explorer — Business Data Analysis Skill

Use this skill when a business user wants to explore, query, and visualize data from Excel files (or CSV) without writing code. The skill turns spreadsheets into a queryable local database and lets users ask questions in plain English.

## When to Activate

- "analyze this Excel file"
- "upload a spreadsheet"
- "what does this data show?"
- "query my data"
- "build a chart from this file"
- "summarize this spreadsheet"
- "find trends in this data"
- "pivot this data"
- "graph this for me"
- "data exploration"
- "mine this dataset"
- User provides an `.xlsx`, `.xls`, or `.csv` file

## When NOT to Use

- Designing production databases (use `architecture-design` skill)
- Building real-time dashboards or BI platforms
- Large-scale ETL or data engineering pipelines
- Cloud database provisioning (use `azure-prepare`)
- Compliance or security reviews (use `compliance-enforcement` skill)

## Prerequisites

The following tools must be available in the environment. The agent should **auto-install missing tools** using the appropriate package manager before proceeding.

| Tool            | Purpose                         | Install Command                                   |
| --------------- | ------------------------------- | ------------------------------------------------- |
| **Python 3.9+** | Runtime for data processing     | Pre-installed or `winget install Python.Python.3` |
| **openpyxl**    | Read `.xlsx` files              | `pip install openpyxl`                            |
| **pandas**      | Data manipulation and analysis  | `pip install pandas`                              |
| **sqlite3**     | Lightweight local database      | Built into Python standard library                |
| **matplotlib**  | Chart generation                | `pip install matplotlib`                          |
| **tabulate**    | Pretty-print tables in terminal | `pip install tabulate`                            |

> **One-liner install**: `pip install openpyxl pandas matplotlib tabulate`

## Core Workflow

### Phase 1: Ingest — Read the Spreadsheet

1. **Detect the file format** — `.xlsx`, `.xls`, or `.csv`
2. **Read all sheets** — For multi-sheet Excel files, list sheets and ask the user which to load (default: all)
3. **Preview the data** — Show the first 5–10 rows of each sheet so the user can confirm it looks right
4. **Handle data quality issues automatically**:
   - Detect and skip empty rows/columns
   - Infer column types (text, number, date, boolean)
   - Flag columns with mixed types or excessive nulls
   - Normalize column names (lowercase, underscores, no special characters)
5. **Report what was found**:

```
📊 Loaded: azure_adoption_tracker.xlsx
   Sheet "FY25 Q3" → 1,247 rows × 8 columns
   Sheet "FY25 Q4" → 1,103 rows × 8 columns

   Columns detected:
   | Column            | Type    | Sample                    | Nulls |
   |-------------------|---------|---------------------------|-------|
   | date              | date    | 2025-01-15                | 0     |
   | business_unit     | text    | "Cloud + AI"              | 0     |
   | service           | text    | "Azure OpenAI Service"    | 12    |
   | active_customers  | number  | 3,420                     | 0     |
   | monthly_revenue   | number  | 2,450,000.00              | 0     |
   | consumption_units | number  | 8,750,000                 | 3     |
   | account_manager   | text    | "Priya Nair"              | 0     |
   | segment           | text    | "Enterprise"              | 0     |
```

### Phase 2: Store — Load into Local SQLite

1. **Create a local SQLite database** at `.copilot-tracking/data-explorer/{filename}.db`
2. **Create tables** — one per sheet, with proper column types
3. **Upsert data** — insert all rows; if the database already exists from a prior run, drop and recreate tables (or upsert if the user specifies a key column)
4. **Create indexes** on columns that look like IDs, dates, or categories (for faster queries)
5. **Confirm to the user**:

```
✅ Data loaded into local database
   Database: .copilot-tracking/data-explorer/azure_adoption_tracker.db
   Table "fy25_q3" → 1,247 rows
   Table "fy25_q4" → 1,103 rows
   Ready for queries!
```

### Phase 3: Query — Natural Language to SQL

When the user asks a question in plain English:

1. **Translate the question to SQL** — use the table schema as context
2. **Show the SQL** (collapsed/optional) so the user can learn or verify
3. **Run the query** against the local SQLite database
4. **Present results** in a clean, readable table
5. **Add a plain-English summary** of what the results mean

**Example interaction:**

> **User**: "Which Azure services had the most customer growth last quarter?"
>
> **Agent**:
>
> ```sql
> SELECT service, SUM(active_customers) as total_customers
> FROM fy25_q4
> GROUP BY service
> ORDER BY total_customers DESC
> LIMIT 5;
> ```
>
> | Azure Service            | Active Customers |
> | ------------------------ | ---------------- |
> | Azure OpenAI Service     | 14,230           |
> | Azure Cosmos DB          | 9,870            |
> | Azure Kubernetes Service | 8,720            |
> | Microsoft Fabric         | 6,450            |
> | Azure AI Search          | 5,210            |
>
> _Azure OpenAI Service led with 14.2K active customers — nearly 45% more than the second-place Cosmos DB. AI services are clearly driving the adoption wave._

**Query capabilities to support:**

| User Asks                                           | SQL Pattern                          |
| --------------------------------------------------- | ------------------------------------ |
| "Show me revenue by business unit"                  | `GROUP BY` with `SUM`                |
| "What's the average consumption per service?"       | `AVG` with `GROUP BY`                |
| "Compare Q3 vs Q4 adoption"                         | `JOIN` or `UNION` across tables      |
| "Which account manager covers the most customers?"  | `COUNT` with `GROUP BY` + `ORDER BY` |
| "Show only services with revenue above $1M"         | `WHERE` filters                      |
| "What's the customer growth trend over time?"       | `GROUP BY` date with aggregation     |
| "Find duplicate customer entries"                   | `GROUP BY` + `HAVING COUNT > 1`      |
| "What percentage of revenue comes from Enterprise?" | Calculated columns with `CASE`       |

### Phase 4: Visualize — Build Charts

When the user asks for a chart or when a visualization would help tell the story:

1. **Choose the right chart type** based on the data and question:

| Data Pattern            | Chart Type   | When to Use                                            |
| ----------------------- | ------------ | ------------------------------------------------------ |
| Categories + values     | Bar chart    | Comparing items (top Azure services, by business unit) |
| Time + values           | Line chart   | Trends over time (monthly customer growth)             |
| Parts of a whole        | Pie chart    | Proportions (segment mix, % by business unit)          |
| Two numeric columns     | Scatter plot | Correlations (consumption vs. revenue)                 |
| Distribution            | Histogram    | Spread of values (customer sizes)                      |
| Categories × Categories | Heatmap      | Cross-tabulations (business unit × service)            |

2. **Generate the chart using matplotlib** — save to `.copilot-tracking/data-explorer/{filename}/charts/`
3. **Always include** a title, axis labels, and data labels where readable
4. **Use business-friendly colors** — no neon, no default matplotlib colors
5. **Present with context** — don't just show the chart; explain what it reveals

**Chart generation pattern (Python):**

```python
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
import sqlite3
import pandas as pd

# Connect to the local database
conn = sqlite3.connect('.copilot-tracking/data-explorer/{filename}.db')

# Run the query
df = pd.read_sql_query("SELECT service, SUM(active_customers) as total FROM fy25_q4 GROUP BY service ORDER BY total DESC LIMIT 6", conn)

# Create the chart
fig, ax = plt.subplots(figsize=(10, 6))
ax.bar(df['service'], df['total'], color=['#0078D4', '#50E6FF', '#00BCF2', '#FFB900', '#D83B01', '#107C10'])
ax.set_title('Azure Service Adoption — FY25 Q4', fontsize=14, fontweight='bold')
ax.set_ylabel('Active Customers')
ax.yaxis.set_major_formatter(ticker.StrMethodFormatter('{x:,.0f}'))
plt.xticks(rotation=30, ha='right')

# Add value labels on bars
for bar in ax.patches:
    ax.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 100,
            f'{bar.get_height():,.0f}', ha='center', fontsize=10)

plt.tight_layout()
plt.savefig('.copilot-tracking/data-explorer/{filename}/charts/adoption_by_service.png', dpi=150)
plt.show()
conn.close()
```

### Phase 5: Summarize — Insight Report (Optional)

If the user wants a shareable summary, generate an **Insight Report** at `.copilot-tracking/data-explorer/{filename}/insight-report.md`:

```markdown
# Data Insight Report: {filename}

**Date**: [today's date]
**Source**: {original file name}
**Rows analyzed**: {total rows}

## Key Findings

1. [Finding 1 — with supporting number]
2. [Finding 2 — with supporting number]
3. [Finding 3 — with supporting number]

## Charts

![Accounts by Segment](charts/accounts_by_segment.png)
![Vertical Growth Trend](charts/vertical_growth_trend.png)

## Data Summary

| Metric         | Value       |
| -------------- | ----------- |
| Total records  | X           |
| Date range     | Start – End |
| [Key metric 1] | [Value]     |
| [Key metric 2] | [Value]     |

## Queries Used

| Question Asked                | Result         |
| ----------------------------- | -------------- |
| "[Natural language question]" | [Brief answer] |
```

## Interaction Style

- **Plain language only** — no jargon, no technical terms unless the user uses them
- **Proactive suggestions** — after showing results, suggest follow-up questions: _"Want to see this broken down by month?"_ or _"Should I chart this?"_
- **Error recovery** — if a query fails, explain what went wrong in simple terms and suggest a rephrased question
- **Iterative** — encourage the user to keep asking questions; each answer often sparks the next question
- **Teach gently** — if the user is curious, briefly explain what SQL was used (but never force it)

## File Organization

```
.copilot-tracking/data-explorer/
├── {filename}.db                        # SQLite database (keep)
├── {filename}_schema.md                 # Auto-generated schema docs (keep)
├── load_{filename}.py                   # Reproducible load script (keep — this is the only .py to persist)
├── scripts/                             # Temporary query/chart scripts (disposable)
│   ├── query_001.py
│   ├── chart_002.py
│   └── ...
└── {filename}/
    ├── insight-report.md                # Summary report (keep)
    └── charts/                          # Generated chart images only — no .py files here
        ├── accounts_by_segment.png
        ├── vertical_growth_trend.png
        └── ...
```

**Script management rules:**

- **Only persist `load_{filename}.py`** — the database load script is the only Python file worth keeping long-term (for reproducibility)
- **Chart scripts go to `scripts/`** — not alongside the PNG output. These are intermediate artifacts.
- **Charts folder contains only images** — `.png` files only, no `.py` files
- When generating a chart, save the `.py` to `scripts/` and the `.png` to `{filename}/charts/`
- At the end of a session, offer to clean up: _"Want me to delete the temporary scripts in `scripts/`? The charts and database are already saved."_

## Guardrails

- **Local only** — data never leaves the user's machine; SQLite is a local file
- **Non-destructive** — original Excel file is never modified
- **PII protection** — always follow the `pii-scrubbing` skill in `.github/skills/pii-scrubbing/SKILL.md`; scan columns on ingest, flag PII columns, mask in query results and charts, generate a PII manifest
- **Size limits** — warn if the file exceeds 100MB or 500K rows; suggest sampling for exploratory queries
- **Schema documentation** — always generate a schema doc so others can understand the database
- **Reproducibility** — save the Python load script so the database can be recreated from the original file

## Handoff Points

| From Data Explorer | To                                         | How                                             |
| ------------------ | ------------------------------------------ | ----------------------------------------------- |
| Insight report     | **Explore & Research** recipe              | Use findings as input for strategic exploration |
| Key findings       | **Idea-to-Prototype** recipe               | Data-backed idea validation                     |
| Charts + report    | **Business-to-Engineering Handoff** recipe | Evidence for engineering investment             |
| Schema + queries   | **Spec-to-Ship** recipe                    | Data requirements carry forward into PRD        |
