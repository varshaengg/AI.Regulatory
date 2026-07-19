---
description: 'Enterprise-grade blueprint generator for creating comprehensive copilot-instructions.md files that guide GitHub Copilot to produce code consistent with Microsoft development standards, enterprise architecture patterns, and exact technology versions by analyzing existing codebase patterns.'
---

# GitHub Copilot Instructions Blueprint Generator for Microsoft Teams

## Configuration Variables

- `PROJECT_TYPE`: Auto-detect|.NET|Java|JavaScript|TypeScript|React|Angular|Python|Azure Functions|Power Platform|Multiple|Other <!-- Primary technology stack -->
- `ARCHITECTURE_STYLE`: Layered|Microservices|Monolithic|Domain-Driven|Event-Driven|Serverless|Cloud-Native|Mixed <!-- Enterprise architectural approach -->
- `CODE_QUALITY_FOCUS`: Maintainability|Performance|Security|Accessibility|Testability|Compliance|All <!-- Quality and compliance priorities -->
- `DOCUMENTATION_LEVEL`: Minimal|Standard|Comprehensive|Enterprise <!-- Documentation requirements -->
- `TESTING_REQUIREMENTS`: Unit|Integration|E2E|TDD|BDD|Security|Performance|All <!-- Testing approach -->
- `VERSIONING`: Semantic|CalVer|Microsoft Internal|Custom <!-- Versioning approach -->
- `MICROSOFT_STACK`: Azure|M365|Power Platform|Mixed|External <!-- Microsoft technology focus -->

## Generated Blueprint

Generate a comprehensive copilot-instructions.md file that will guide GitHub Copilot to produce enterprise-quality code consistent with Microsoft development standards, security requirements, and technology versions. This blueprint ensures alignment with Microsoft's engineering practices while respecting existing codebase patterns.

### Enterprise Approach

#### 1. Core Enterprise Instruction Structure

```markdown
# GitHub Copilot Instructions - Microsoft Development Standards

## Priority Guidelines for Microsoft Teams

When generating code for this Microsoft repository:

1. **Microsoft Technology Alignment**: Prioritize Microsoft technologies and services where appropriate
2. **Security First**: Always apply Microsoft security best practices and compliance requirements
3. **Version Compatibility**: Detect and respect exact versions of languages, frameworks, and libraries
4. **Enterprise Context**: Prioritize patterns defined in .github/copilot directory and enterprise standards
5. **Codebase Consistency**: Maintain our ${ARCHITECTURE_STYLE} architectural style and established patterns
6. **Quality Standards**: Emphasize ${CODE_QUALITY_FOCUS == "All" ? "maintainability, performance, security, accessibility, compliance, and testability" : CODE_QUALITY_FOCUS} in all code

## Microsoft Technology Stack Integration

### Azure Integration (if applicable)
- Prioritize Azure services for cloud solutions
- Follow Azure Well-Architected Framework principles
- Use Azure SDK patterns and best practices
- Implement Azure security and monitoring standards
- Apply Azure cost optimization patterns

### Microsoft 365 Integration (if applicable)
- Follow Microsoft Graph API patterns
- Apply Microsoft 365 security and compliance standards
- Use Teams development best practices
- Implement SharePoint Framework (SPFx) patterns where applicable

### Power Platform Integration (if applicable)
- Follow Power Platform development best practices
- Apply Power Apps and Power Automate patterns
- Use Dataverse security models
- Implement Power Platform governance standards

## Technology Version Detection and Compliance

Before generating code, perform comprehensive analysis:

1. **Language and Runtime Versions**:
   - Detect exact versions from project configuration files
   - Ensure compatibility with Microsoft-supported versions
   - Follow Microsoft's language version adoption guidelines
   - Never use features beyond detected or enterprise-approved versions

2. **Framework and Library Versions**:
   - Analyze package managers (NuGet, npm, pip, etc.)
   - Respect enterprise-approved library versions
   - Follow Microsoft's dependency management best practices
   - Ensure compatibility with enterprise security scanning tools

3. **Microsoft Technology Versions**:
   - Detect specific versions of Azure SDKs, .NET frameworks, etc.
   - Apply version-specific patterns and recommendations
   - Follow Microsoft's technology lifecycle policies

## Enterprise Context Files

Prioritize these files in .github/copilot directory (when present):

- **enterprise-architecture.md**: Enterprise system architecture and patterns
- **microsoft-tech-stack.md**: Approved Microsoft technologies and versions
- **security-standards.md**: Enterprise security requirements and patterns
- **coding-standards.md**: Enterprise code style and quality standards
- **compliance-requirements.md**: Regulatory and compliance guidelines
- **folder-structure.md**: Enterprise project organization standards
- **exemplars.md**: Approved code patterns and reference implementations

## Enterprise Codebase Analysis

When enterprise context files don't provide specific guidance:

1. **Pattern Analysis**: Identify and follow established enterprise patterns
2. **Security Pattern Recognition**: Apply consistent security implementations
3. **Architecture Boundary Respect**: Maintain clear separation of concerns
4. **Enterprise Convention Following**:
   - Naming conventions aligned with enterprise standards
   - Error handling consistent with enterprise logging
   - Documentation matching enterprise requirements
   - Testing approaches meeting enterprise quality gates

## Microsoft Security and Compliance Standards

### Security Requirements
- Follow Microsoft Security Development Lifecycle (SDL) practices
- Implement defense-in-depth security patterns
- Apply principle of least privilege consistently
- Use Microsoft-approved cryptographic libraries and patterns
- Follow data classification and handling requirements
- Implement proper authentication and authorization patterns

### Compliance Considerations
- Ensure GDPR compliance for data handling
- Follow industry-specific regulations (HIPAA, SOX, etc.)
- Apply Microsoft compliance framework patterns
- Implement audit logging consistent with enterprise requirements
- Follow data residency and sovereignty requirements

## Enterprise Code Quality Standards

### Enterprise Maintainability (if applicable)
- Apply SOLID principles consistently with enterprise patterns
- Follow Microsoft coding conventions and style guides
- Implement comprehensive error handling and logging
- Use dependency injection patterns matching enterprise standards
- Create self-documenting code with enterprise naming conventions

### Enterprise Performance (if applicable)
- Follow Microsoft performance best practices
- Implement efficient resource utilization patterns
- Apply async/await patterns consistently
- Use appropriate caching strategies for enterprise scale
- Follow scalability patterns for cloud deployment

### Enterprise Security (if applicable)
- Implement comprehensive input validation and sanitization
- Use parameterized queries and safe API patterns
- Apply proper secret management using Azure Key Vault or similar
- Follow enterprise authentication and authorization patterns
- Implement comprehensive audit logging

### Enterprise Compliance (if applicable)
- Follow data governance and retention policies
- Implement proper data classification handling
- Apply regulatory compliance patterns
- Ensure audit trail completeness
- Follow enterprise data protection standards

## Enterprise Documentation Standards

### Documentation Level Requirements
**Enterprise**: Comprehensive enterprise documentation including Microsoft documentation standards, API documentation, security considerations, deployment guides, troubleshooting information, compliance documentation, and architecture decision records (ADRs).

**Comprehensive**: Detailed documentation with complete API documentation, public interfaces, usage examples, security and performance considerations.

**Standard**: Standard documentation following existing patterns with public API documentation, key functionality, parameter descriptions, and basic usage examples.

**Minimal**: Minimal documentation matching existing comment style, documenting non-obvious behavior and basic parameter descriptions.

## Enterprise Testing Standards

### Security Testing (if applicable)
- Implement comprehensive security test coverage
- Include penetration testing considerations
- Apply threat modeling to test design
- Test authentication and authorization scenarios
- Validate input sanitization and data protection

### Performance Testing (if applicable)
- Include load and stress testing considerations
- Implement performance benchmarking
- Test scalability scenarios
- Validate resource utilization patterns

### Unit Testing (if applicable)
- Achieve high code coverage following enterprise standards
- Use dependency injection for testability
- Follow arrange-act-assert patterns
- Implement comprehensive mocking strategies

### Integration Testing (if applicable)
- Test enterprise system integrations
- Validate API contracts and service boundaries
- Test error handling and resilience patterns
- Verify security and compliance requirements

## Microsoft Technology-Specific Guidelines

### .NET Enterprise Guidelines (if applicable)
- Follow Microsoft .NET coding standards and conventions
- Use dependency injection container patterns (Microsoft.Extensions.DependencyInjection)
- Implement comprehensive logging using Microsoft.Extensions.Logging
- Apply configuration management using Microsoft.Extensions.Configuration
- Use Entity Framework Core for data access where appropriate
- Follow async/await best practices for scalability
- Implement proper exception handling and error boundaries

### Azure Functions Enterprise Guidelines (if applicable)
- Follow Azure Functions best practices and patterns
- Implement proper dependency injection and configuration
- Use durable functions for workflow scenarios
- Apply proper monitoring and telemetry
- Follow security best practices for serverless applications

### Power Platform Enterprise Guidelines (if applicable)
- Follow Power Platform governance and best practices
- Apply proper solution layering and dependencies
- Use environment variables for configuration management
- Implement proper security roles and data loss prevention
- Follow Power Platform ALM best practices

## Enterprise DevOps and Deployment

### CI/CD Pipeline Standards
- Follow Microsoft-recommended DevOps practices
- Implement comprehensive automated testing in pipelines
- Apply security scanning and compliance validation
- Use infrastructure as code (Bicep/ARM templates)
- Implement proper environment promotion strategies

### Monitoring and Observability
- Implement comprehensive logging using enterprise standards
- Apply distributed tracing for microservices
- Use Application Insights or equivalent monitoring
- Implement proper alerting and incident response
- Follow enterprise monitoring and dashboard standards

## Version Control and Collaboration

### Versioning Standards
**Microsoft Internal**: Follow Microsoft's internal versioning standards with proper branching strategies and semantic versioning with enterprise considerations.

**Semantic**: Follow semantic versioning principles with clear API change documentation and proper deprecation strategies.

### Code Review Standards
- Follow Microsoft's code review best practices
- Ensure security review for all code changes
- Apply architecture review for significant changes
- Validate compliance and documentation standards

## Enterprise Compliance and Governance

### Data Governance
- Follow enterprise data classification schemes
- Implement proper data retention and deletion
- Apply data sovereignty and residency requirements
- Ensure GDPR and regulatory compliance

### Security Governance
- Follow enterprise security policies and standards
- Implement proper access controls and permissions
- Apply security by design principles
- Ensure vulnerability management compliance

## Microsoft-Specific Best Practices

- Prioritize Microsoft technologies for new implementations
- Follow Microsoft Well-Architected Framework principles
- Apply Microsoft's enterprise architecture patterns
- Use Microsoft-recommended security practices
- Follow Microsoft's sustainability and green development practices
- Implement proper telemetry and diagnostic capabilities
- Apply Microsoft's accessibility standards and guidelines

## Project-Specific Enterprise Guidance

- Conduct thorough enterprise architecture review before major changes
- Ensure alignment with enterprise technology roadmap
- Validate security and compliance requirements
- Follow enterprise change management processes
- Apply proper risk assessment and mitigation strategies
- Ensure business continuity and disaster recovery considerations

#### 2. Enterprise Codebase Analysis Protocol

To create enterprise-grade copilot-instructions.md files:

**Enterprise Technology Assessment:**
- Identify all Microsoft technologies and services in use
- Validate against enterprise-approved technology stack
- Document integration patterns with Microsoft ecosystem
- Assess compliance with Microsoft licensing and support

**Security and Compliance Analysis:**
- Identify security patterns and implementations
- Document compliance requirements and implementations
- Validate against enterprise security standards
- Assess data governance and protection measures

**Enterprise Architecture Review:**
- Analyze alignment with enterprise architecture patterns
- Document service boundaries and integration points
- Validate scalability and performance patterns
- Assess monitoring and observability implementations

**Quality and Standards Assessment:**
- Document coding standards and quality gates
- Analyze testing coverage and strategies
- Validate documentation completeness
- Assess DevOps and deployment patterns

#### 3. Implementation Guidelines

The enterprise copilot-instructions.md should:

- Be placed in the .github/copilot directory for team access
- Reference Microsoft-approved patterns and technologies
- Include explicit security and compliance requirements
- Avoid external dependencies without enterprise approval
- Provide Microsoft-specific examples and patterns
- Be comprehensive yet maintainable for enterprise use
- Include proper governance and review processes

## Expected Output

A comprehensive, enterprise-grade copilot-instructions.md file that guides GitHub Copilot to produce code fully aligned with Microsoft's development standards, security requirements, and enterprise architecture patterns while maintaining perfect compatibility with existing technology versions and established codebase patterns.
