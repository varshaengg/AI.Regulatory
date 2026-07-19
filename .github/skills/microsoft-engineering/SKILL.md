---
name: microsoft-engineering
description: "Microsoft internal development best practices including Azure Well-Architected Framework, Managed Identity authentication, OneBranch/Ev2 deployment, and enterprise engineering standards. USE FOR: Microsoft best practices, Azure Well-Architected, managed identity, ManagedIdentityCredential, OneBranch deployment, Ev2 deployment, progressive rollout, deployment rings, security scanning, Guardian, component governance pipeline, enterprise CI/CD, code signing, Azure DevOps pipelines, ring deployment, health check, rollback strategy. DO NOT USE FOR: specific coding standards (use language instructions), architecture HLD design (use architecture-design skill), compliance policy review (use compliance-enforcement skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Microsoft Engineering Best Practices

Comprehensive best practices for Microsoft internal teams covering system architecture, code development, testing, deployment (OneBranch/Ev2), security, monitoring, and documentation using Microsoft products and services.

## When to Activate

- "Microsoft best practices"
- "Azure Well-Architected"
- "managed identity" / "ManagedIdentityCredential"
- "OneBranch deployment"
- "Ev2 deployment"
- "progressive rollout"
- "deployment rings"
- "enterprise CI/CD pipeline"
- "Guardian security scan"
- "code signing"
- Building Azure-hosted services with Microsoft tooling

## When NOT to Use

- Language-specific coding standards (use language instructions)
- High-level architecture design (use `architecture-design` skill)
- Compliance/privacy policy review (use `compliance-enforcement` skill)
- Cross-repo workflows (use `cross-repo-development` skill)

## Azure Well-Architected Framework

Follow the five pillars:

| Pillar                     | Key Practices                                                                   |
| -------------------------- | ------------------------------------------------------------------------------- |
| **Reliability**            | Availability Zones, circuit breakers, graceful degradation, Azure Site Recovery |
| **Security**               | Azure AD, Zero Trust, Key Vault, Security Center                                |
| **Cost Optimization**      | Cost Management, auto-scaling, reserved instances                               |
| **Operational Excellence** | IaC (Bicep/Terraform), GitOps, Azure Monitor                                    |
| **Performance Efficiency** | Load Balancer, caching (Redis), CDN, DB optimization                            |

## Authentication Standards (CRITICAL)

### Managed Identity — Primary Authentication Method

**ALWAYS** use Managed Identity credentials for connecting to Azure resources. **NEVER** use secrets, certificates, or `DefaultAzureCredential` in production code.

- **System-Assigned Managed Identity**: Single-resource scenarios
- **User-Assigned Managed Identity**: Multi-resource or cross-subscription scenarios
- **Federated Identity Credential (FIC)**: Token generation and external system integration

```csharp
// .NET: Correct way
var credential = new ManagedIdentityCredential(clientId);
var client = new SecretClient(new Uri(keyVaultUrl), credential);
```

```python
# Python: Correct way
credential = ManagedIdentityCredential(client_id=client_id)
client = SecretClient(vault_url=key_vault_url, credential=credential)
```

```typescript
// TypeScript: Correct way
const credential = new ManagedIdentityCredential(clientId);
const client = new SecretClient(keyVaultUrl, credential);
```

### What NOT to Use

- `DefaultAzureCredential` (too broad, unpredictable credential chain)
- Connection strings with secrets
- Service principal secrets or certificates
- Shared access signatures (SAS) for long-term access
- Any hardcoded credentials or keys

## Architecture Patterns

### Microservices

- Azure Container Instances or AKS
- Azure API Management for gateways
- Azure Service Bus or Event Grid for inter-service communication
- Domain-driven design principles

### Event-Driven

- Azure Event Grid for routing
- Azure Service Bus for reliable messaging
- CQRS with Azure Cosmos DB
- Azure Functions for event processing

### Serverless

- Azure Functions for compute
- Azure Logic Apps for orchestration
- Azure API Management for API exposure

## Code Development Standards

### .NET

- Target latest stable .NET version (8+)
- Dependency injection with built-in container
- SOLID principles, clean architecture
- Nullable reference types, proper async/await

### Python

- Python 3.9+ with type hints
- PEP 8 style guidelines
- Virtual environments (venv or conda)
- Proper error handling and logging

### TypeScript

- Strict TypeScript for type safety
- ESLint and Prettier configurations
- Modern ES6+ features

### Code Quality

- Minimum 80% code coverage
- Static code analysis (SonarQube, CodeQL)
- Code reviews for all changes
- EditorConfig for consistent formatting

## Testing Strategy

| Type            | Framework                          | Focus                                      |
| --------------- | ---------------------------------- | ------------------------------------------ |
| **Unit**        | XUnit/.NET, pytest/Python, Jest/TS | 80%+ coverage, AAA pattern, mock externals |
| **Integration** | TestContainers, real dependencies  | API endpoints, Azure service integrations  |
| **E2E**         | Playwright, Selenium               | Critical user journeys, cross-browser      |
| **Performance** | Azure Load Testing                 | Load/stress, performance budgets, SLAs     |
| **Security**    | OWASP ZAP, dependency scanning     | Auth/authz, input sanitization             |

## Deployment Platforms

For detailed deployment configuration, see [references/deployment-best-practices.md](references/deployment-best-practices.md).

### OneBranch

- Microsoft's unified build and deployment system
- Security by default: isolated environments, no persistent state
- Built-in compliance scanning, code signing
- Zero Trust architecture

### Ev2 (Express V2)

- Enterprise deployment platform for Azure services
- Progressive deployment with automatic rollback
- Health monitoring and approval gates
- Deep integration with Azure Resource Manager

### Deployment Ring Strategy

| Ring                | Scope | Duration | Checks                   |
| ------------------- | ----- | -------- | ------------------------ |
| Ring 0 (Canary)     | 1%    | 1 hour   | Aggressive health checks |
| Ring 1 (Pilot)      | 10%   | 4 hours  | Standard monitoring      |
| Ring 2 (Production) | 100%  | 12 hours | Standard monitoring      |

### Pipeline Security Requirements

- **MANDATORY**: Use Managed Identity for all Azure resource connections
- **FORBIDDEN**: Never use DefaultAzureCredential or hardcoded secrets
- **REQUIRED**: Security scanning (Guardian, Component Governance)
- **REQUIRED**: Federated Identity Credential for external integrations

## Security & Compliance

### Identity and Access Management

- Azure AD for all authentication
- ManagedIdentityCredential for Azure resource connections
- FIC for external system integration
- Conditional Access policies, PIM, MFA

### Data Protection

- Azure Information Protection for data classification
- Encryption at rest and in transit
- Key Vault for certificates/keys (accessed via Managed Identity only)
- Data residency requirements

### Network Security

- Azure Firewall and NSGs
- Private endpoints for Azure services
- Azure Bastion for secure VM access
- DDoS protection

## Monitoring & Observability

### Application Insights

- Comprehensive telemetry, custom metrics
- Proactive alerting, user behavior monitoring

### Azure Monitor

- Log Analytics for centralized logging
- Custom dashboards and workbooks
- Automated alert responses

### Health Checks

- Application health endpoints
- Application Gateway health probes
- Downstream dependency monitoring
- Synthetic monitoring

## Code Review Checklist

- [ ] .NET code follows SOLID principles and coding standards
- [ ] Python code follows PEP 8 and includes proper documentation
- [ ] **CRITICAL**: All Azure resource connections use ManagedIdentityCredential
- [ ] **SECURITY**: No hardcoded secrets, certificates, or connection strings
- [ ] **AUTHENTICATION**: FIC is properly configured for external integrations
- [ ] Security requirements met for the technology stack
- [ ] Performance considerations addressed

## Internal Resources

- [Microsoft Engineering Playbook](https://eng.ms/)
- [Azure Architecture Center](https://docs.microsoft.com/azure/architecture/)
- [Microsoft SDL](https://www.microsoft.com/securityengineering/sdl/)
- [OneBranch](https://aka.ms/onebranch)
- [Ev2 Platform](https://aka.ms/ev2)
