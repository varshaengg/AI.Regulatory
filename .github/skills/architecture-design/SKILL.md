---
name: architecture-design
description: "High-Level Design (HLD) workflow for Software Architects. Multi-step technology discovery, options analysis, and architecture deliverables. USE FOR: architecture design, HLD, high-level design, system architecture, technology stack discovery, infrastructure analysis, architecture patterns, options analysis, trade-offs, migration strategy. DO NOT USE FOR: coding standards (use language-specific instructions), deployment execution (use microsoft-engineering skill), compliance review (use compliance-enforcement skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# High-Level Design (HLD) — Architect Skill

Use this skill when operating in the Software Architect role to produce HLDs for any repository. Deliver designs that are actionable, compatible with the current stack, and incrementally adoptable.

## When to Activate

- "design the architecture"
- "create a high-level design"
- "HLD for this feature"
- "evaluate architecture options"
- "technology stack analysis"
- "migration strategy"
- Architecture review or proposal tasks

## When NOT to Use

- Writing code to specific language standards (use language instructions)
- Executing deployments (use `microsoft-engineering` skill)
- Compliance/security reviews (use `compliance-enforcement` skill)
- Cross-repo stitching (use `cross-repo-development` skill)

## Dynamic Repository Context Analysis (MANDATORY FIRST STEP)

Before starting any HLD work, perform comprehensive repository analysis to understand the current technology landscape:

### 1. Technology Stack Discovery

Analyze these indicators to determine the tech stack:

- Package/dependency files: `package.json`, `requirements.txt`, `pom.xml`, `*.csproj`, `Gemfile`, `go.mod`, `Cargo.toml`, `composer.json`
- Configuration files: `Dockerfile`, `docker-compose.yml`, `.env`, config files
- Framework indicators: `next.config.js`, `angular.json`, `vue.config.js`, `webpack.config.js`, `tsconfig.json`
- Language distribution: `*.py`, `*.js`, `*.ts`, `*.java`, `*.cs`, `*.go`, `*.rb`, `*.php`, `*.rs`
- Database configs: migrations, schema files, connection configs, ORM configurations
- API specifications: OpenAPI/Swagger files, GraphQL schemas, proto files

### 2. Infrastructure & Platform Analysis

Identify deployment and infrastructure patterns:

- Cloud platform: AWS, Azure, GCP, or on-premise indicators
- Container orchestration: Kubernetes manifests, Docker Swarm, container configs
- CI/CD pipelines: `.github/workflows/`, `.gitlab-ci.yml`, `azure-pipelines.yml`, `Jenkinsfile`
- Infrastructure as Code: Terraform, CloudFormation, Pulumi, ARM templates, CDK, Bicep
- Serverless: Lambda functions, Azure Functions, Cloud Functions
- Monitoring: Observability tools, logging frameworks, metrics collection

### 3. Architecture Pattern Recognition

Understand current architectural approaches:

- Monorepo vs multi-repo: `lerna.json`, `nx.json`, workspace configurations
- Microservices vs monolith: service directories, API gateway configs, service mesh
- Event-driven: Message queues, event streaming, pub/sub patterns
- Domain organization: DDD patterns, bounded contexts, module structure
- Data patterns: CQRS, Event Sourcing, database-per-service, shared databases

### 4. Current Constraints & Dependencies

Identify existing constraints that must be preserved:

- Framework versions and compatibility matrix
- Database schemas and migration strategies
- External service integrations and contracts
- Security and compliance requirements (GDPR, HIPAA, SOX indicators)
- Performance SLAs and monitoring thresholds
- Existing architectural decisions and technical debt

### 5. Development Ecosystem Analysis

Understand the development lifecycle and tooling:

- Build systems: npm/yarn scripts, Maven, Gradle, make, CMake, bazel
- Testing frameworks: Jest, pytest, JUnit, RSpec, Go test, Rust test
- Code quality: ESLint, Prettier, SonarQube, CodeClimate, linters
- Package management: npm, pip, Maven, NuGet, Go modules, Cargo
- Environment management: Docker, virtualenv, nvm, rbenv, pyenv

## HLD Workflow (Technology-Agnostic)

### 1) Requirements Clarification

- **Business goals**: Functional requirements, user stories, business value
- **Scope definition**: System boundaries, integration points, data flows
- **Success criteria**: KPIs, SLIs, SLOs, acceptance criteria
- **Non-functional requirements**:
  - Performance: latency targets, throughput requirements, scalability needs
  - Reliability: availability targets, disaster recovery, fault tolerance
  - Security: authentication, authorization, data protection, compliance
  - Operational: monitoring, logging, deployment, maintenance requirements

### 2) Technology-Aware Analysis

Based on discovered technology stack, consider:

#### For Web Applications

- Frontend: Component architecture, state management, routing, build optimization
- Backend: API design patterns, middleware architecture, database integration
- Full-stack: SSR/SSG considerations, API integration patterns, deployment strategies

#### For Cloud-Native Applications

- Containerization: Docker strategies, image optimization, orchestration patterns
- Serverless: Function granularity, cold start optimization, event-driven patterns
- Microservices: Service boundaries, communication patterns, data consistency

#### For Data-Intensive Applications

- Storage: Database selection, data modeling, partitioning, backup strategies
- Processing: Batch vs stream processing, data pipeline architecture, ETL patterns
- Analytics: Data warehouse, lake house, real-time analytics, reporting

### 3) Options Analysis & Trade-offs

- Draft 2-3 viable architectural approaches
- Evaluate against discovered technology constraints
- Consider migration complexity from current state
- Assess team capabilities and organizational readiness
- Analyze cost implications and resource requirements

### 4) Architecture Deliverables

Generate comprehensive HLD documentation including:

#### Core Architecture

- **Problem statement**: Clear problem definition and goals
- **Architecture overview**: High-level narrative and visual diagrams
- **Component breakdown**: Services, modules, dependencies, responsibilities
- **Data architecture**: Storage, flow patterns, consistency models
- **Integration patterns**: APIs, messaging, event handling

#### Technical Specifications

- **Technology stack**: Languages, frameworks, databases, infrastructure
- **Deployment architecture**: Environments, scaling, infrastructure requirements
- **Security architecture**: Authentication, authorization, data protection
- **Monitoring & observability**: Logging, metrics, tracing, alerting strategies

#### Implementation Planning

- **Migration strategy**: Phased approach, rollback plans, risk mitigation
- **Development workflow**: Build, test, deploy, monitor processes
- **Operational considerations**: Runbooks, incident response, maintenance
- **Performance planning**: Load testing, capacity planning, optimization strategies

## Edit/Commit Protocol

### 1) Proposal Phase

- Intent summary, scope definition, change outline
- Goal alignment with business and technical objectives
- Assumptions being made

### 2) Critical Analysis

- Requirements coverage, compatibility validation, risk assessment
- Security review, performance impact, cost analysis
- Alternative evaluation with rationale

### 3) Integration & Validation

- Constraint compliance, testing strategy, monitoring plan
- Rollback procedures, documentation updates

### 4) Implementation Readiness

- Change log with rationale, risks, mitigations
- Acceptance criteria, timeline, resource requirements

## Architecture Patterns Reference

### Microservices

- Components: API Gateway, Service Registry, Config Server, Services
- Communication: REST, gRPC, GraphQL, Message Queues
- Data: Database per service, Event sourcing, CQRS
- Infrastructure: Containers, Service Mesh, Load Balancers

### Event-Driven

- Components: Event Store, Message Brokers, Event Processors
- Patterns: Pub/Sub, Event Sourcing, Saga, CQRS
- Technologies: Kafka, RabbitMQ, Redis, Cloud messaging
- Considerations: Eventual consistency, ordering, deduplication

### Serverless

- Components: Functions, API Gateway, Event Sources, Storage
- Patterns: Function composition, Event-driven, Backend-as-a-Service
- Considerations: Cold starts, statelessness, vendor lock-in

### Data Architecture

- Patterns: Data Lake, Data Warehouse, Lambda Architecture, Kappa
- Storage: Relational, NoSQL, Graph, Time-series, Object storage
- Processing: Batch, Stream, Real-time, ETL/ELT

## Compatibility Constraints

- Respect existing framework and library version constraints
- Maintain existing API contracts and service interfaces
- Preserve data formats and communication protocols
- Maintain or improve current performance benchmarks
- Maintain current security posture and compliance requirements

## Validation Framework

### Testing Strategy

- Unit, integration, performance, security, and E2E tests

### Monitoring & Observability

- Metrics, logging, distributed tracing, alerting

### Success Criteria

- Functional, performance, reliability, security, and operational targets met

## Migration & Rollout Strategy

- **Phase 0**: Analysis, planning, preparation
- **Phase 1**: Core infrastructure and foundational changes
- **Phase 2**: Feature implementation and integration
- **Phase 3**: Performance optimization and scaling
- **Phase 4**: Full deployment and legacy decommissioning

### Risk Mitigation

- Feature flags, blue/green deployments, canary releases
- Circuit breakers, rollback procedures
