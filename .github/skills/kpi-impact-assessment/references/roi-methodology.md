<!-- markdownlint-disable-file -->

# ROI Methodology — Calculation Framework

Standardized methodology for calculating return on investment in impact assessments.

## Core Formula

```
ROI % = ((Total Annual Returns - Total Annual Investment) / Total Annual Investment) × 100
```

## Cost Categories

### One-Time Costs (Year 1 Only)

| Category | What to Include | Estimation Method |
|----------|----------------|-------------------|
| **Licensing setup** | Initial license purchase, subscription activation | Vendor pricing or estimate |
| **Implementation** | Configuration, integration, customization labor | Hours × loaded rate |
| **Migration** | Data migration, system cutover, parallel running | Hours × loaded rate |
| **Training** | Formal training sessions, self-paced learning, ramp time | Hours per person × headcount × loaded rate |
| **Opportunity cost** | Productivity dip during transition (learning curve) | 2–4 weeks at 30% reduced productivity × team size × loaded rate |

### Recurring Costs (Annual)

| Category | What to Include | Estimation Method |
|----------|----------------|-------------------|
| **Licensing** | Annual subscription or per-seat fees | Vendor pricing × seats |
| **Maintenance** | Patches, upgrades, support contracts | Typically 15–20% of license cost |
| **Operations** | Dedicated admin/ops time for the platform | FTE fraction × loaded rate |
| **Infrastructure** | Cloud compute, storage, networking costs | Azure pricing calculator or actuals |

## Benefit Categories

### Direct Cost Savings

Reduction in existing spend that can be traced to a budget line:

- **Labor savings** — Hours automated × loaded hourly rate
- **Infrastructure savings** — Reduced compute, storage, or licensing
- **Vendor consolidation** — Eliminated tools or duplicate platforms
- **Incident cost reduction** — Fewer incidents × average cost per incident

### Productivity Gains

Time freed for higher-value work (not headcount reduction unless stated):

- **Developer productivity** — Hours saved per developer per week × team size × 48 work weeks
- **Process acceleration** — Reduced cycle time × throughput increase
- **Reduced context switching** — Fewer tool switches × time-per-switch × frequency
- **Faster onboarding** — Days saved per new hire × hires per year × daily loaded rate

### Risk Avoidance

Costs that would be incurred without the initiative:

- **Security breach avoidance** — Probability × average breach cost ($4.45M per IBM 2024)
- **Compliance penalty avoidance** — Probability × fine amount
- **Downtime avoidance** — Reduced MTTR × hourly revenue impact
- **Technical debt interest** — Avoided rework hours from accumulating debt

### Strategic Value (Qualitative with Proxies)

Benefits that are real but harder to quantify directly:

- **Time-to-market acceleration** — Weeks saved × estimated revenue per week of earlier launch
- **Talent retention** — Reduced attrition × cost-per-replacement ($150K–$250K per engineer)
- **Competitive positioning** — Qualitative, referenced in executive summary only

## Loaded Cost Rates

Default hourly rates for labor calculations. Override with actuals when available.

| Role | Default Loaded Rate | Notes |
|------|-------------------|-------|
| Junior IC (L59–L61) | $75/hr | Includes benefits, overhead |
| Mid IC (L62–L63) | $85/hr | Use as default when role is unspecified |
| Senior IC (L64–L65) | $120/hr | |
| Principal IC (L66+) | $160/hr | |
| Engineering Manager | $140/hr | |
| Director | $180/hr | |
| VP/CVP | $250/hr | Typically only for executive time in meetings |

**Loaded rate formula:** `base_salary × 1.35 / 2,080 hours` (35% burden for benefits, taxes, overhead)

## Confidence Adjustments

Apply risk-adjusted discounts to all projected returns:

| Confidence Level | Discount | When to Apply |
|-----------------|----------|---------------|
| **High** — measured baseline, proven technology | 10% | Direct measurement from materials, mature solution |
| **Medium** — reasonable estimate | 20% | Conservative assumptions, user-provided context |
| **Low** — estimated baseline, unproven approach | 35% | No baseline data, new technology, uncertain adoption |

**Default:** Apply **20% confidence discount** to all projected returns unless the user specifies otherwise.

```
Risk-Adjusted Returns = Gross Returns × (1 - Confidence Discount)
```

## Payback Period

```
Payback Period (months) = Total Year-1 Investment / (Risk-Adjusted Annual Returns / 12)
```

- **< 6 months** — Strong case, fast payback
- **6–12 months** — Good case, typical for technology investments
- **12–18 months** — Acceptable for strategic initiatives
- **> 18 months** — Requires strong strategic justification

## Net Present Value (3-Year)

Calculate 3-year NPV using the specified discount rate (default: 8%):

```
NPV = Σ (Net Benefit in Year t) / (1 + discount_rate)^t - Initial Investment

Where:
  Year 0 = -Initial Investment (one-time costs)
  Year 1 = Risk-Adjusted Returns - Recurring Costs
  Year 2 = Risk-Adjusted Returns × 1.1 - Recurring Costs  (10% growth from maturity)
  Year 3 = Risk-Adjusted Returns × 1.15 - Recurring Costs (15% growth from full adoption)
```

**Growth assumptions:** Returns grow modestly in Years 2–3 as adoption matures and efficiency compounds. Use 10% Year 2 growth and 15% Year 3 growth as defaults.

## Sensitivity Analysis (Optional)

For high-stakes assessments, include a sensitivity table showing ROI under different scenarios:

| Scenario | Adoption Rate | Savings Realized | ROI % | Payback |
|----------|--------------|-----------------|-------|---------|
| **Conservative** | 50% adoption | 60% of projected savings | X% | X months |
| **Base case** | 75% adoption | 80% of projected savings | X% | X months |
| **Optimistic** | 95% adoption | 100% of projected savings | X% | X months |

## Common Pitfalls

- **Double-counting** — Do not count the same benefit in multiple categories
- **Ignoring adoption curve** — Full benefits rarely appear in Month 1; model a 3–6 month ramp
- **Omitting opportunity cost** — Training time and productivity dip during transition are real costs
- **Hero numbers** — If the ROI exceeds 500%, recheck assumptions; it may be inflated
- **Missing recurring costs** — Licensing, maintenance, and ops are ongoing, not one-time
