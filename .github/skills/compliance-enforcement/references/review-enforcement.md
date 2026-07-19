# Compliance — Code Review / PR Enforcement

Operational procedures for validating privacy, security, and compliance during code review and PR validation.

## Activation

Run on every PR and code review to detect and remediate compliance gaps before merge.

## Responsibilities

- Scan codebase for telemetry/logging/data flow points
- Validate all events are mapped in `privacy-manifest.json`
- Surface missing annotations, manifest entries, or violations
- Scan for S360 gaps (privacy, security, compliance, flagged packages, CodeQL, CG, SDL, threat model)
- Block PRs if any S360 KPI is not met
- Provide actionable remediation guidance

## Privacy Enforcement

- Detect telemetry/logging calls (e.g., `TrackEvent`, `logger.Log*`)
- Check if each event is listed in `privacy-manifest.json`
- If missing, suggest an entry with: EventName, DataCategory, RetentionDays, Justification
- Annotate code with privacy attributes if applicable

## Security Enforcement

- Validate secure-by-design principles in all code changes
- Detect and resolve CodeQL bugs
- Avoid use of deprecated or vulnerable packages
- Ensure license compliance for all dependencies
- Validate inputs using data annotations or FluentValidation

## Accessibility Review

Review code for accessibility issues (a11y):
- Missing or incorrect ARIA attributes
- Improper heading structure
- Insufficient color contrast
- Missing alt text for images
- Non-accessible form labels
- Keyboard navigation traps
- Hardcoded font sizes without relative units
- WCAG 2.1 violations

For each issue: specify file/line, explain the problem, suggest the exact fix.

## PR Behavior

On every PR:
1. Run all validation tasks
2. Add comments if issues are found
3. Block merge if:
   - Telemetry is unmapped
   - Security scan fails
   - Compliance check fails
4. Validate mitigations are complete, implemented, documented
5. Flag missing threat model updates

## Threat Model & DFD Review

- If no threat model exists, generate one identifying all potential threats with Mermaid.js diagrams
- If new endpoints or data stores are introduced, update `docs/dfd.md` and `docs/threatmodel.md`
- Review and identify security vulnerabilities in the codebase
- Document all findings and remediation steps

## Manifest Update Suggestion

If telemetry is added but not mapped:
```json
{
  "EventName": "NewEventName",
  "DataCategory": "Pseudonymous",
  "RetentionDays": 30,
  "Justification": "Used for optimization and audit"
}
```

## Inline Comment Format

```
Violation: <Policy-Area>
Location: <file>:<line>
Issue: <short description>
Policy: <link to policy doc>
Fix: <exact fix or patch snippet>
Evidence: <optional: validation commands>
```

## Example Enforcement Comment

```
CG Alert: `Newtonsoft.Json 12.x` is deprecated.
[View Alert](https://alert-url-from-s360)
Recommendation: Upgrade to `13.0.1`.
```
