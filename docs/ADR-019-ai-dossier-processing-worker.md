# ADR-019: Durable AI Dossier Processing Worker

**Status:** Proposed  
**Date:** 2026-09-29  
**Decision owners:** Product owner, solution architect, engineering lead  
**Related design:** [AI Dossier Preparation - Detailed Design](AI-Dossier-Preparation-Design.md)

## Context

Preparing a dossier requires repository traversal, document download, extraction,
OCR, Azure OpenAI classification, embeddings, Search indexing, slot mapping, gap
analysis, and package compilation. A run may process many large documents and
take substantially longer than an interactive HTTP request.

The current solution hosts a React SPA and ASP.NET Core API on App Service. The
API currently returns seeded dossier-run data and has no durable AI worker,
Service Bus, or Document Intelligence integration.

## Decision

Implement AI dossier preparation as a separate .NET worker consuming durable
Azure Service Bus messages.

The API creates and controls runs but does not perform long-running document
processing in request threads. Azure SQL stores durable state; Blob Storage
stores extracted content and artifacts. The worker uses Managed Identity to call
Storage, SQL, Document Intelligence, Azure OpenAI, and Azure AI Search.

The first implementation is limited to a Module 3 compile-only vertical slice.
AI may parse template structure, classify documents, and disambiguate slot
assignments, but it may not author regulatory narrative. Human approval is
required before compiling the dossier package.

The worker hosting technology should support independent scaling and native
Service Bus event scaling. Azure Container Apps is preferred because it supports
container reuse with the existing .NET delivery model and KEDA-based scaling.

## Alternatives considered

### Process work inside the ASP.NET Core API

Rejected. App Service restarts, request timeouts, scale changes, and deployment
slots make long-running in-process work difficult to resume and audit reliably.

### Azure Functions

Viable, but not selected as the preferred target. Functions provide strong
Service Bus integration, but introduce a separate hosting and programming model.
They remain a fallback if organizational standards prefer Functions.

### Synchronous client-orchestrated processing

Rejected. Browser lifetime and network reliability cannot own a regulated,
multi-stage processing workflow.

## Consequences

### Positive

- Processing survives API and browser restarts.
- Stages retry independently and dead-letter permanent failures.
- AI/OCR workloads scale separately from interactive API traffic.
- Durable state and stage events provide auditability and progress reporting.
- The same worker model can expand from Module 3 to all CTD modules.

### Negative

- New Service Bus, Document Intelligence, worker compute, deployment, and
  monitoring resources are required.
- Messages and stage handlers must be idempotent.
- Operational ownership expands beyond the existing two App Services.
- Cost controls and concurrency limits are required for OCR and model usage.

## Constraints

- Managed Identity only; no embedded service keys.
- No AI-authored regulatory narrative in the first release.
- No template structure becomes active without Admin approval.
- No dossier package is compiled without RA Lead approval.
- Every model output is schema validated and version traceable.
- Every run locks its template, source, rule, prompt, and model versions.

## Validation

The decision is validated when a Module 3 run can be interrupted and resumed
without duplicated documents or assignments, permanent failures appear in a
dead-letter workflow, and the final package is reproducible from persisted run
metadata.
