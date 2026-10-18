# ADR-020: Use GPT-4.1-mini as the default Azure OpenAI chat model

**Status:** Accepted

**Date:** 2026-10-18

**Decision owner:** Product owner

**Related requirement:** FR-031 — configurable model quality and cost

**Related design:** [Software Design Document](SDD.md), sections 2.3, 4.6, 5.1, and 5.2

## Context

The infrastructure deployment failed because Azure rejected the configured
`gpt-4o-mini` model version `2024-07-18` as deprecating and unavailable for new
deployments. The API also used that deployment name as its fallback. The model
catalog lists `gpt-4.1-mini` version `2025-04-14` as an Azure OpenAI chat model,
and Microsoft's regional availability documentation lists it in South India.

The user approved replacing the default model on 2026-10-18.

## Decision

Use Azure OpenAI `gpt-4.1-mini` version `2025-04-14` as the default chat
deployment. Name the deployment `gpt-4.1-mini` and keep the existing
`text-embedding-3-large` deployment. Use the new model name consistently in the
Bicep deployment, API fallback, and specification.

## Rationale

- The previous model version cannot be newly deployed.
- The selected model is available in the configured Azure region and supports
  chat completions.
- Keeping the chat model separate from the embedding model preserves current
  application behavior and deployment responsibilities.

## Consequences

- Existing prompt behavior may differ; classification and JSON-output behavior
  must be evaluated before production use.
- The API's configured deployment override remains supported.
- Existing deployments are updated in place under the deployment name
  `gpt-4.1-mini`; no code path should depend on the old deployment name.

## Validation

- Bicep compilation succeeds and renders model `gpt-4.1-mini`, version
  `2025-04-14`, with the configured deployment SKU.
- The API and worker builds pass.
- The infrastructure workflow and downstream deployment/smoke checks succeed.

## References

- [Azure OpenAI model availability](https://learn.microsoft.com/azure/ai-foundry/openai/concepts/models#model-summary-table-and-region-availability)
- [Azure OpenAI model lifecycle](https://learn.microsoft.com/azure/ai-foundry/openai/concepts/model-retirements)
