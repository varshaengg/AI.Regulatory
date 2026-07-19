# Component Governance (CG) Policy

This file defines **common Component Governance (CG) alerts**, deprecated packages, and **S360-exported active vulnerabilities** (as of last sync).
Agents should use this knowledge to **flag issues during code generation and code review** and provide actionable recommendations.

---

## 1. Common Alerts & Vulnerabilities (Static Rules)

### Deprecated Packages
- **Newtonsoft.Json** < 13.0.1: Upgrade to latest
- **System.Text.Encodings.Web** < 4.7.2: Upgrade to latest stable version
- **jQuery** < 3.5.0: XSS vulnerabilities
- **Log4j** < 2.17.1: CVE-2021-44228 (Log4Shell)
- **Bootstrap** < 4.3.1: XSS issues

### Security Vulnerable Packages
- **SharpZipLib** < 1.3.3: ZipSlip vulnerability
- **System.Net.Http** < 4.3.4: DoS vulnerability
- **YamlDotNet** < 11.2: Deserialization risk
- **NPM minimist** < 1.2.5: Prototype pollution
- **NPM lodash** < 4.17.21: Security issues

### Transitive Dependencies
- Packages pulled indirectly via other dependencies
  - Example: `Microsoft.AspNetCore.All` pulls **EntityFrameworkCore** which may have its own CVEs
  - Agents must recursively check and report

---

## 2. Recommendations

- Always run:
  - `dotnet list package --vulnerable`
  - `npm audit` (for JavaScript repos)
  - `pip-audit` (for Python repos)
- Cross-reference against:
  [Fixing Component Governance Alerts](https://eng.ms/docs/experiences-devices/m365-core-ic3/ic3-platform/data-platform/ic3-data-platform-tsgs/common/sop/general/fixing-component-governance-alerts)
- Use SBOM & CG checks before dependency installation
- Reject packages with critical/high vulnerabilities
- Reject dependencies not in allowlist:
  - Outdated NuGet packages
  - Deprecated NPM libraries
  - Insecure crypto algorithms

---

## 3. Expected Agent Behavior

Whenever agent is generating code, or when asked to review code via PR or repo scan:

1. **Detect** any deprecated / vulnerable / transitive dependency
2. **Cross-reference** against:
   - Section 1 (static rules)
   - Section 4 (S360 active alerts, if available)
3. **Comment inline** on risky code/package with:
   - File & line reference
   - Issue detected
   - Related **AlertUrl**
   - Recommended upgrade or mitigation steps

---

## References
- https://docs.opensource.microsoft.com/tools/cg/index.html
- Internal guidance: [Fixing Component Governance Alerts](https://eng.ms/docs/experiences-devices/m365-core-ic3/ic3-platform/data-platform/ic3-data-platform-tsgs/common/sop/general/fixing-component-governance-alerts)
