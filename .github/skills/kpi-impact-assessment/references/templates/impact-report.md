<!-- markdownlint-disable-file -->

# Impact Assessment Report Template

Full report template combining all phases of the KPI Impact Assessment skill. Save completed reports to `.copilot-tracking/impact-assessments/{{initiative-slug}}-impact-assessment.md`.

---

# Impact Assessment: {{Initiative Name}}

**Date:** {{YYYY-MM-DD}}
**Author:** {{Role or team}}
**Audience:** {{Executive / Engineering Lead / PM}}
**Time Horizon:** {{12 months / 24 months / 36 months}}
**Status:** Draft | Review | Final

---

## Executive Summary

{{300-word executive summary from Phase 5. See [executive-summary.md](executive-summary.md) for template.}}

---

## KPI Framework

### Selected Metrics ({{count}})

| # | KPI | Category | Baseline | Target | Δ | Measurement Method | Data Source | Cadence | Owner |
|---|-----|----------|----------|--------|---|-------------------|-------------|---------|-------|
| 1 | {{KPI name}} | {{category}} | {{value}} | {{value}} | {{%}} | {{method}} | {{source}} | {{cadence}} | {{role}} |
| 2 | | | | | | | | | |
| 3 | | | | | | | | | |
| 4 | | | | | | | | | |
| 5 | | | | | | | | | |
| 6 | | | | | | | | | |
| 7 | | | | | | | | | |
| 8 | | | | | | | | | |

### Category Coverage

- [ ] Velocity — at least 1 KPI
- [ ] Quality — at least 1 KPI
- [ ] Cost — at least 1 KPI
- [ ] Risk or Compliance — at least 1 KPI

---

## Improvement Opportunities

| # | Opportunity | Current State | Improved State | $ Savings/yr | Hours Saved/yr | % Improvement | Key Assumptions |
|---|-------------|---------------|----------------|-------------|----------------|---------------|-----------------|
| 1 | | | | | | | |
| 2 | | | | | | | |
| 3 | | | | | | | |
| 4 | | | | | | | |
| 5 | | | | | | | |

### Totals

| Metric | Value |
|--------|-------|
| **Total Annual Savings** | ${{sum}} |
| **Total Hours Freed/Year** | {{sum}} hours |
| **Average % Improvement** | {{avg}}% |

---

## ROI Calculation

### Investment Costs (Year 1)

| Category | Item | One-Time | Recurring/yr |
|----------|------|----------|-------------|
| Licensing | {{description}} | ${{amount}} | ${{amount}} |
| Implementation | {{description}} | ${{amount}} | — |
| Training | {{description}} | ${{amount}} | — |
| Operations | {{description}} | — | ${{amount}} |
| Infrastructure | {{description}} | — | ${{amount}} |
| **Totals** | | **${{sum}}** | **${{sum}}** |

**Total Year 1 Investment:** ${{one-time + recurring}}

### Projected Returns (Year 1, Risk-Adjusted)

| Category | Gross Value | Confidence Discount | Risk-Adjusted Value |
|----------|------------|--------------------|--------------------|
| Direct cost savings | ${{amount}} | {{%}} | ${{amount}} |
| Productivity gains | ${{amount}} | {{%}} | ${{amount}} |
| Risk avoidance | ${{amount}} | {{%}} | ${{amount}} |
| Quality improvements | ${{amount}} | {{%}} | ${{amount}} |
| **Totals** | **${{sum}}** | | **${{sum}}** |

### Summary

| Metric | Value |
|--------|-------|
| **Total Year 1 Investment** | ${{amount}} |
| **Risk-Adjusted Annual Returns** | ${{amount}} |
| **Net Annual Benefit** | ${{amount}} |
| **ROI %** | {{%}} |
| **Payback Period** | {{X.X}} months |
| **3-Year NPV (8% discount)** | ${{amount}} |
| **Break-Even Point** | Month {{X}} |

---

## Risk of Inaction

Delaying or declining this initiative carries compounding costs:

### Financial Exposure

{{Description of continued costs, growth rate, and 12/36-month projections}}

### Competitive Gap

{{Description of market position risk, competitor adoption rates, delivery speed differential}}

### Operational Fragility

{{Description of technical debt accumulation, incident probability growth, system degradation}}

### Talent Risk

{{Description of developer satisfaction impact, attrition correlation, replacement cost}}

### Cost of Inaction Summary

| Time Horizon | Estimated Cost |
|-------------|---------------|
| **12 months** | ${{amount}} |
| **36 months** | ${{amount}} |

---

## Appendix A: Assumptions and Data Sources

| # | Assumption | Value Used | Source | Confidence |
|---|-----------|-----------|--------|------------|
| 1 | {{description}} | {{value}} | {{source}} | High/Medium/Low |
| 2 | | | | |
| 3 | | | | |

---

## Appendix B: Methodology Notes

- **ROI calculation** follows standard Total Economic Impact methodology
- **Confidence discount** of {{%}} applied to all projected returns
- **Loaded cost rates** sourced from {{source or "user-provided data"}}
- **KPI baselines** sourced from {{materials provided or "N/A if unavailable"}}
- **3-year NPV** calculated at {{%}} discount rate with {{%}} Year 2 and {{%}} Year 3 growth assumptions
- **Risk of inaction** costs calculated using {{methodology description}}

---

## Appendix C: KPI Measurement Plan

| KPI | Measurement Tool | Dashboard/Report | Responsible Role | First Measurement Date |
|-----|-----------------|-----------------|-----------------|----------------------|
| {{name}} | {{tool}} | {{link or name}} | {{role}} | {{date}} |

---

*Report generated using the KPI Impact Assessment skill — GHC AI Accelerators*
