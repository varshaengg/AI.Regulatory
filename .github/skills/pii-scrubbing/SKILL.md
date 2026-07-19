---
name: pii-scrubbing
description: "Cross-cutting PII detection and scrubbing guardrails for all agents, recipes, and outputs. Automatically identifies, masks, and redacts personally identifiable information in inputs, outputs, artifacts, logs, and generated code. Ensures compliance with GDPR, CCPA, and Microsoft privacy standards. USE FOR: PII detection, PII scrubbing, data masking, redaction, personal data removal, privacy protection, anonymization, pseudonymization, sensitive data handling, GDPR compliance, CCPA compliance, data sanitization, scrub outputs, mask names, remove emails, hide phone numbers. DO NOT USE FOR: encryption at rest or in transit (use security tooling), access control or RBAC (use azure-rbac), full compliance audits (use compliance-enforcement skill), threat modeling (use security-plan-creator agent)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# PII Scrubbing — Privacy-First Data Handling

This is a **cross-cutting skill** that ensures every agent, recipe, and output in the accelerator handles personally identifiable information (PII) responsibly. It applies to **all contexts** — engineering recipes, business recipes, data exploration, code generation, document generation, and agent interactions.

**The rule is simple:** No PII should appear in any generated artifact, log, example, or output unless the user explicitly authorizes it for a specific, justified purpose.

## When to Activate

- **Always.** This skill is active for every agent and recipe by default.
- Any time user-provided data, enterprise data, or external data is being processed
- When generating code samples, examples, or test data
- When writing documents, PRDs, insight briefs, or reports
- When querying databases (including Data Explorer / SQLite)
- When pulling context from Teams, M365, SharePoint (via WorkIQ MCP)
- When creating ADO work items, PRs, or any external-facing artifacts
- "scrub PII", "mask data", "anonymize", "remove personal info", "redact names"

## When NOT to Use

- Encrypting data at rest or in transit (use proper encryption tooling)
- Configuring RBAC or access controls (use `azure-rbac` skill)
- Full compliance audits (use `compliance-enforcement` skill)
- Threat modeling (use `security-plan-creator` agent)

## PII Categories & Detection

### What Counts as PII

Agents must scan for and handle these categories:

| Category                | Examples                                             | Risk Level |
| ----------------------- | ---------------------------------------------------- | ---------- |
| **Names**               | Full names, usernames, aliases, display names        | High       |
| **Email addresses**     | Personal or corporate email (user@domain.com)        | High       |
| **Phone numbers**       | Mobile, office, fax — any format                     | High       |
| **Physical addresses**  | Street, city, ZIP, full mailing addresses            | High       |
| **Government IDs**      | SSN, passport, driver's license, tax ID, national ID | Critical   |
| **Financial data**      | Credit card numbers, bank accounts, IBAN             | Critical   |
| **Health information**  | Medical records, diagnoses, insurance IDs            | Critical   |
| **Dates of birth**      | Full DOB, age when combined with other PII           | Medium     |
| **IP addresses**        | IPv4, IPv6 when tied to individuals                  | Medium     |
| **Biometric data**      | Fingerprints, facial recognition, voice prints       | Critical   |
| **Device identifiers**  | MAC addresses, device IDs when tied to individuals   | Medium     |
| **Location data**       | GPS coordinates, geolocation history                 | Medium     |
| **Authentication data** | Passwords, tokens, API keys, secrets                 | Critical   |
| **Employee IDs**        | Internal employee/contractor identifiers             | Medium     |
| **Customer IDs**        | When combined with other identifying information     | Medium     |

### Detection Patterns

Agents should use these patterns to detect PII in text, data, and code:

```
# Email
[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}

# Phone (international formats)
(\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}

# SSN (US)
\b\d{3}-\d{2}-\d{4}\b

# Credit card
\b(?:\d{4}[-\s]?){3}\d{4}\b

# IP Address (v4)
\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b

# US ZIP Code
\b\d{5}(?:-\d{4})?\b

# Date of Birth patterns
\b(?:DOB|Date of Birth|Born|Birthday)\s*:?\s*\d{1,2}[/-]\d{1,2}[/-]\d{2,4}\b
```

> **Note:** Pattern matching alone is insufficient. Agents must also use **contextual analysis** — a string like "John Smith" is PII when it's a real person's name but not when it's a variable name in code documentation. Apply judgment.

## Scrubbing Rules

### Rule 1: Detect Before Output

Before writing **any** artifact (document, code file, work item, chart title, SQL result, log entry), scan the content for PII.

```
INPUT → PII Detection → PII Found?
                            ├── YES → Apply scrubbing rules → OUTPUT (clean)
                            └── NO  → OUTPUT (as-is)
```

### Rule 2: Scrubbing Methods (Choose Appropriately)

| Method               | How It Works                                              | When to Use                                                        |
| -------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ |
| **Redaction**        | Replace with `[REDACTED]`                                 | When the value is irrelevant to the output (e.g., SSN in a report) |
| **Masking**          | Partially hide: `j***@contoso.com`, `***-**-1234`         | When the format matters but the full value doesn't                 |
| **Pseudonymization** | Replace with realistic fake: "Jane Smith" → "Alex Morgan" | When examples need to look real (PRDs, scenarios, test data)       |
| **Generalization**   | Reduce precision: "42 years old" → "40-50 age range"      | When aggregate data is sufficient                                  |
| **Tokenization**     | Replace with a reversible token: `[USER_001]`             | When the original value might need to be recovered (rare)          |
| **Removal**          | Delete entirely                                           | When the PII adds no value to the output                           |

### Rule 3: Context-Appropriate Defaults

| Context                                          | Default Method                            | Rationale                                                |
| ------------------------------------------------ | ----------------------------------------- | -------------------------------------------------------- |
| **Generated documents** (PRDs, briefs, reports)  | Pseudonymization                          | Docs need to read naturally                              |
| **Code generation** (examples, tests, seed data) | Pseudonymization with fake data libraries | Tests need realistic but fake data                       |
| **SQL query results** (Data Explorer)            | Masking                                   | User sees patterns without raw PII                       |
| **Charts and visualizations**                    | Generalization or aggregation             | Visuals shouldn't expose individuals                     |
| **ADO work items**                               | Redaction or removal                      | Work items are visible to teams                          |
| **Teams/M365 context** (WorkIQ MCP)              | Redaction                                 | Summarize decisions, not who said what (unless relevant) |
| **Logs and tracking files**                      | Removal                                   | Logs should never contain PII                            |
| **Handoff packages**                             | Pseudonymization                          | Engineering needs realistic context                      |

### Rule 4: Explicit Opt-In for PII Retention

If a user **explicitly needs** to include PII (e.g., a report about a specific customer), the agent must:

1. **Ask for confirmation**: _"This output will include personal information (names and email addresses). Are you sure you want to include it?"_
2. **Warn about downstream effects**: _"Note: if this document is shared or stored in ADO, the PII will be visible to others with access."_
3. **Tag the artifact**: Add a header to the file:
   ```markdown
   > ⚠️ **PII Notice**: This document contains personally identifiable information.
   > Handle according to your organization's data classification policy.
   > Generated: [date] | Authorized by: [user]
   ```

### Rule 5: Fake Data Generation

When agents need to generate sample, seed, or test data, use **realistic but entirely fictional** data:

**Microsoft-themed fake data library:**

| Category      | Fake Examples                                                                 |
| ------------- | ----------------------------------------------------------------------------- |
| **Names**     | Alex Morgan, Priya Kapoor, Sam Chen, Jordan Rivera, Taylor Kim                |
| **Emails**    | alex.morgan@contoso.com, priya.k@fabrikam.com, schen@northwindtraders.com     |
| **Companies** | Contoso Ltd, Fabrikam Inc, Northwind Traders, Adventure Works, Woodgrove Bank |
| **Domains**   | contoso.com, fabrikam.com, northwindtraders.com, adventureworks.com           |
| **Addresses** | 1 Microsoft Way, Redmond WA 98052 (use only the public Microsoft HQ address)  |
| **Phone**     | (425) 555-0100 through (425) 555-0199 (555 numbers are reserved for fiction)  |
| **IDs**       | EMP-10001, CUST-A2024, ACCT-7890 (clearly synthetic patterns)                 |
| **Dates**     | Use dates in the past that don't correspond to real events                    |

> **Never** use real Microsoft employee names, real customer data, or real internal IDs as examples.

### Rule 6: Data Explorer Special Handling

When the `data-explorer` skill loads Excel/CSV data:

1. **Scan on ingest**: After reading the spreadsheet but before displaying the preview, scan all columns for PII patterns
2. **Flag PII columns**: Alert the user:
   _"I noticed columns that may contain personal information: 'employee_name', 'email', 'phone'. I'll mask these in query results and charts. You can tell me to include them if needed."_
3. **Create a PII manifest**: Record which columns contain PII at `.copilot-tracking/data-explorer/{filename}_pii-manifest.md`:
   ```markdown
   # PII Manifest: {filename}

   | Column        | PII Type | Handling                    |
   | ------------- | -------- | --------------------------- |
   | employee_name | Name     | Pseudonymized in outputs    |
   | email         | Email    | Masked (j\*\*\*@domain.com) |
   | phone         | Phone    | Redacted                    |
   ```
4. **Mask in query results**: When PII columns appear in results, apply the appropriate method
5. **Exclude from charts**: Never put individual PII on chart axes or labels — aggregate instead

### Rule 7: WorkIQ MCP & Teams Data

When pulling context from Teams, M365, and SharePoint:

- **Summarize conversations** rather than quoting individuals verbatim (unless the user asks for attribution)
- **Attribute by role**, not name, when possible: _"The engineering lead mentioned..."_ rather than _"John Smith said..."_
- **Never surface** email addresses, phone numbers, or calendar details of others without explicit need
- **Strip participant lists** from meeting summaries unless relevant to the question

### Rule 8: Code Generation

When generating code that handles data:

- **Never hardcode real PII** in examples, tests, or seed data
- Always use the fake data library (Rule 5)
- Generate code with **PII-safe patterns**:

  ```python
  # ✅ Good — uses faker or constants for test data
  test_user = {"name": "Alex Morgan", "email": "alex.morgan@contoso.com"}

  # ❌ Bad — looks like it could be real data
  test_user = {"name": "John D. Smith", "email": "jdsmith@microsoft.com"}
  ```

- When generating data access code, include comments about PII handling:
  ```python
  # NOTE: This query returns PII columns (name, email).
  # Ensure results are masked before displaying or logging.
  ```

## Reporting & Audit Trail

### PII Scan Summary

At the end of any recipe that processes external data, generate a brief PII summary:

```markdown
## PII Handling Summary

- **Data sources scanned**: [list]
- **PII columns detected**: [count and names]
- **Handling applied**: [scrubbing methods used]
- **User opt-ins**: [any explicit authorizations]
- **Artifacts with PII notices**: [list any tagged files]
```

### Compliance Alignment

This skill aligns with:

| Standard                       | How This Skill Helps                                                    |
| ------------------------------ | ----------------------------------------------------------------------- |
| **GDPR** (EU)                  | Data minimization, purpose limitation, pseudonymization                 |
| **CCPA** (California)          | Consumer data protection, right to deletion support                     |
| **Microsoft Privacy Standard** | Aligns with Microsoft's internal data handling policies                 |
| **S360 KPIs**                  | Supplements the `compliance-enforcement` skill for runtime PII handling |
| **HIPAA** (if applicable)      | Health data de-identification through safe harbor method                |

## How Agents Reference This Skill

Every agent in the accelerator should include:

```
Always follow the `pii-scrubbing` skill in `.github/skills/pii-scrubbing/SKILL.md`. Scan all inputs and outputs for PII. Apply appropriate scrubbing (redaction, masking, pseudonymization) before generating artifacts. Never include real PII in examples, test data, or sample code.
```

## Applicability Matrix

| Component                           | This Skill Active?                 | Primary Method                        |
| ----------------------------------- | ---------------------------------- | ------------------------------------- |
| **All agents**                      | ✅ Always                          | Context-appropriate (see Rule 3)      |
| **All recipes**                     | ✅ Always                          | Context-appropriate (see Rule 3)      |
| **Data Explorer**                   | ✅ Always — with enhanced scanning | Masking + PII manifest                |
| **Explore & Research** (WorkIQ)     | ✅ Always                          | Redaction + role-based attribution    |
| **Idea-to-Prototype**               | ✅ Always                          | Pseudonymization                      |
| **Business-to-Engineering Handoff** | ✅ Always                          | Pseudonymization                      |
| **Business Case Builder**           | ✅ Always                          | Generalization for financial data     |
| **Stakeholder Communicator**        | ✅ Always                          | Redaction in exec materials           |
| **PRD agent**                       | ✅ Always                          | Pseudonymization in user stories      |
| **Code generation** (any agent)     | ✅ Always                          | Fake data library                     |
| **Compliance Enforcement**          | ✅ Complementary                   | Works alongside, not instead of       |
| **copilot-instructions.md**         | ✅ Referenced                      | Global reference for all interactions |
