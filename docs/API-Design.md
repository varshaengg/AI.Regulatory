# API Design — AI.Regulatory.API

**Companion to:** `SDD.md` (see §3 Logical Architecture, §4 Modules, §9 Security)
**Owner:** Backend team
**Version:** 0.1 (initial draft, aligned to SDD v1.5)

This document specifies the REST/SSE contract exposed by the `AI.Regulatory.API` ASP.NET Core service. It complements — not replaces — the functional module descriptions in `SDD.md` §4.

## Table of Contents

1. [Design principles](#1-design-principles)
2. [Base URL, versioning, hosting](#2-base-url-versioning-hosting)
3. [Authentication & authorization](#3-authentication--authorization)
4. [Common response envelope](#4-common-response-envelope)
5. [Error contract](#5-error-contract)
6. [Pagination, filtering, sorting](#6-pagination-filtering-sorting)
7. [Concurrency (ETag / If-Match)](#7-concurrency-etag--if-match)
8. [Idempotency](#8-idempotency)
9. [Streaming (SSE) — Copilot](#9-streaming-sse--copilot)
10. [File upload / download](#10-file-upload--download)
11. [Endpoint catalog](#11-endpoint-catalog)
    - 11.1 [Health & bootstrap](#111-health--bootstrap)
    - 11.2 [M1 — Auth & user profile](#112-m1--auth--user-profile)
    - 11.3 [M2 — Projects](#113-m2--projects)
    - 11.4 [M3 — CTD templates](#114-m3--ctd-templates)
    - 11.5 [M4 — Document source configuration](#115-m4--document-source-configuration)
    - 11.6 [M5 — Discovery & OCR](#116-m5--discovery--ocr)
    - 11.7 [M6 — Classification](#117-m6--classification)
    - 11.8 [M7 — CTD mapping & dossier arrangement](#118-m7--ctd-mapping--dossier-arrangement)
    - 11.9 [M8 — Gap analysis](#119-m8--gap-analysis)
    - 11.10 [M9 — Regulatory Copilot](#1110-m9--regulatory-copilot)
    - 11.11 [M10 — Reporting](#1111-m10--reporting)
    - 11.12 [M11 — Admin](#1112-m11--admin)
    - 11.13 [M12 — Dossier compilation](#1113-m12--dossier-compilation)
12. [OpenAPI generation](#12-openapi-generation)
13. [Rate limits & quotas](#13-rate-limits--quotas)
14. [Change log](#14-change-log)

---

## 1. Design principles

| Principle | Applied as |
|---|---|
| **REST-first, RPC-when-natural** | Resources are nouns (`/projects`, `/dossiers`); imperative actions become sub-resources (`POST /dossiers/{id}/compile`, `POST /runs/{id}:cancel`). |
| **JSON is the wire format** | `application/json; charset=utf-8` unless streaming (SSE) or binary (uploads). |
| **camelCase over the wire, PascalCase in C#** | ASP.NET Core `JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase`. |
| **DateTime is ISO-8601 UTC** with `Z` suffix | E.g., `2026-04-01T12:34:56Z`. SQL stores `datetime2` UTC. |
| **IDs are string GUIDs** | 36-char lower-case; opaque to clients. |
| **Never surface DB IDs** as URL primary keys when the resource is exposed by a business identifier | e.g., CTD sections use dotted codes (`3.2.S.4.2`) as the URL id, GUID lives internally. |
| **Immutable audit** | Once created, mutations produce a new revision; no destructive UPDATE on approved artefacts (dossiers, sign-offs). |

## 2. Base URL, versioning, hosting

- **Hosted** on the customer's Azure App Service (`AS_API` in SDD §3.1) — Linux container, ASP.NET Core 8.
- Reached from the browser through the SPA App Service's nginx reverse proxy at `/api/*` and `/bff/*` (SDD §3.1, ADR-022).
- **Versioning** — URI-based (`/api/v1/...`). Breaking changes bump the segment; additive changes are v-1 backward compatible. The `/bff/*` prefix has no version segment (session/config only).
- **CORS** — the API rejects cross-origin browser requests by default; only same-origin traffic (through the SPA nginx proxy) is allowed. Developer tooling opens `http://localhost:5173` in Development environment.
- **Correlation** — every request includes an `x-correlation-id` header on the way out and echoes any inbound value; logs and App Insights tag it.

## 3. Authentication & authorization

The SDD (ADR-020) specifies a BFF-cookie pattern as the target design. During the current v0.x phase we run an **interim variant**:

> **Interim (v0.x — matches the current SPA):**
> - The SPA uses **MSAL Browser** (single-tenant `ucatalyst.onmicrosoft.com` in dev) and acquires an access token silently for the API's scope.
> - The API validates the token as a **JWT Bearer** using `Microsoft.Identity.Web`, expecting audience = the API's client ID and issuer = the configured tenant.
> - Roles come from Entra ID app-role claims (`admin`, `raLead`, `raAuthor`, `raReviewer`); permissions are enforced via `[Authorize(Roles = "...")]` and per-endpoint policies.
>
> **Target (v1.0 — matches ADR-020, deferred):**
> - Browser holds only an **HttpOnly, SameSite=Strict, Secure cookie**; MSAL runs in the BFF process.
> - `/bff/login`, `/bff/logout`, `/bff/user` on the ASP.NET Core process; downstream API calls use in-process token exchange.
>
> The switch is transparent to endpoint contracts — only the authentication middleware changes. Endpoints continue to see `ClaimsPrincipal User` regardless.

**Authorization policies** (declared in `Program.cs`):

| Policy | Requirement |
|---|---|
| `AdminOnly` | role `admin` |
| `RaLeadOrAdmin` | role `raLead` OR `admin` |
| `AuthorScope` | role `raAuthor` AND assigned to the section referenced in route |
| `ReviewerScope` | role `raReviewer` AND assigned to the dossier referenced in route |

## 4. Common response envelope

Successful responses are **unwrapped** — the resource IS the response body. This maximises readability, keeps OpenAPI schemas simple, and lets clients cache with ETag directly.

```json
// GET /api/v1/projects/px-102
{
  "id": "px-102",
  "name": "Elmiravir 50 mg",
  "country": "DE",
  "createdAt": "2026-03-14T09:12:03Z",
  "etag": "\"3f2b1a\""
}
```

Collection responses are wrapped in a `page` envelope (see §6).

## 5. Error contract

All non-2xx responses use **RFC 9457 Problem Details** (`application/problem+json`) with the following additions:

```json
{
  "type": "https://ai-regulatory.example/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "The request body contained one or more invalid fields.",
  "instance": "/api/v1/projects",
  "traceId": "00-1a2b3c4d5e6f7089-90abcdef12345678-01",
  "errors": [
    { "field": "country", "code": "required", "message": "country is required" }
  ]
}
```

**Standard error `type` slugs** (append to `https://ai-regulatory.example/errors/`):

| slug | HTTP | Meaning |
|---|---|---|
| `validation` | 400 | Payload failed schema/rule validation |
| `unauthenticated` | 401 | No or invalid bearer token |
| `forbidden` | 403 | Authenticated but lacks role/scope |
| `not-found` | 404 | Resource does not exist |
| `conflict` | 409 | ETag mismatch or business rule (e.g., duplicate) |
| `precondition-failed` | 412 | Preconditions header didn't match |
| `unprocessable` | 422 | Well-formed but semantically wrong (e.g., cannot compile a dossier in `Draft` state) |
| `too-many-requests` | 429 | Rate limit exceeded |
| `internal` | 500 | Unhandled server error (never surfaces stack trace to client) |
| `upstream` | 502 | Downstream (SQL / Blob / AOAI / Graph) failure |
| `timeout` | 504 | Downstream call exceeded budget |

## 6. Pagination, filtering, sorting

Collections use **cursor-based pagination** for large result sets (discovery items, section rows). Simple resources use **offset pagination** for the first N pages.

```
GET /api/v1/projects/px-102/sections?pageSize=50&cursor=eyJvIjozMDB9
```

Response envelope:

```json
{
  "items": [ ... ],
  "page": {
    "pageSize": 50,
    "nextCursor": "eyJvIjozNTB9",
    "hasMore": true
  }
}
```

- `pageSize`: 1..200 (default 50)
- `cursor` is opaque, server-issued
- **Sorting**: `?sort=name,-createdAt` (comma-separated; `-` = descending). Whitelist per endpoint.
- **Filtering**: explicit query params only (`?status=active&owner=alice@…`), never a query DSL. Complex filters are POST search endpoints.

## 7. Concurrency (ETag / If-Match)

Mutable resources return a strong ETag (`"3f2b1a"`) on GET. Clients must send it back on PUT/PATCH:

```
PATCH /api/v1/projects/px-102
If-Match: "3f2b1a"
```

- Mismatch → **412 Precondition Failed**.
- Missing `If-Match` on a PUT/PATCH → **428 Precondition Required**.

## 8. Idempotency

`POST` requests that create resources accept an optional `Idempotency-Key` header (client-generated GUID). The server records `(key, requestHash, response)` for 24 h. Repeats with the same key return the original response.

Required for: `POST /dossiers`, `POST /runs`, `POST /discovery`, `POST /copilot/threads`, `POST /sign-offs`.

## 9. Streaming (SSE) — Copilot

`POST /api/v1/copilot/threads/{threadId}/messages` opens a **Server-Sent Events** stream (`text/event-stream`). See SDD §5.4 and ADR-021.

**Event stream (each `event:` line specifies the event type):**

```
event: token
data: {"delta":"The analytical procedure "}

event: citation
data: {"n":1,"source":"PX-102_M3_QualityDataset.docx","page":47,"section":"3.2.S.4.2"}

event: token
data: {"delta":"employed for characterisation "}

event: done
data: {"messageId":"c8f7…","promptTokens":842,"completionTokens":231}

event: error
data: {"type":"upstream","status":502,"detail":"AOAI rate-limited"}
```

Clients must handle event types **`token`, `citation`, `tool_call`, `done`, `error`**. Unknown event types are ignored.

- **Cancellation** — client closes the `EventSource`; server observes the `HttpContext.RequestAborted` cancellation token and aborts the AOAI call.
- **Retry** — SSE `Last-Event-ID` header is honoured on reconnect; the server replays from that offset within a 60-second window.

## 10. File upload / download

- **Small uploads (≤ 25 MB)** — `multipart/form-data` to `POST /api/v1/{scope}/documents`.
- **Large uploads** — **direct-to-blob via SAS**:
  1. `POST /api/v1/uploads/sas` returns `{ uploadUrl, blobPath, expiresAt }` (SAS write, 15 min).
  2. Client PUTs the file to `uploadUrl` (Azure Blob).
  3. `POST /api/v1/{scope}/documents/register` with `blobPath` to attach metadata + trigger discovery.
- **Downloads** — API returns a short-lived (read) SAS URL; clients download directly from blob to avoid API bandwidth.

## 11. Endpoint catalog

Legend:
- **Method** — HTTP verb
- **Path** — under `/api/v1` unless noted
- **Auth** — `A`=Admin, `L`=RA Lead, `U`=Author, `R`=Reviewer, `∀`=any authenticated
- **Idem** — Idempotency-Key required

### 11.1 Health & bootstrap

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/health/live` | anonymous | Liveness probe (K8s/App Service). |
| GET | `/health/ready` | anonymous | Readiness — checks SQL, Blob, Key Vault. |
| GET | `/bff/config` | anonymous | SPA runtime config: tenant, api base, feature flags, branding tokens. (SDD §3.4) |
| GET | `/bff/user` | ∀ | Current user profile (name, email, roles, avatar) — replaces MSAL client claim reads once BFF cookie switchover happens. |

### 11.2 M1 — Auth & user profile

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/me` | ∀ | Full profile including assigned projects, role scopes. |
| GET | `/api/v1/me/preferences` | ∀ | UI preferences (theme, nav collapse). |
| PUT | `/api/v1/me/preferences` | ∀ | Update UI preferences. |
| GET | `/api/v1/users` | A | Admin: list all users in tenant. |
| POST | `/api/v1/users/{id}:invite` | A | Send B2B guest invite (if multi-tenant). |

### 11.3 M2 — Projects

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/projects` | ∀ | List projects visible to caller (filter by status, owner, country). |
| POST | `/api/v1/projects` | A, L | Create a new project (idempotent). |
| GET | `/api/v1/projects/{id}` | ∀ | Read project. |
| PATCH | `/api/v1/projects/{id}` | A, L | Update (If-Match required). |
| DELETE | `/api/v1/projects/{id}` | A | Archive (soft delete). |
| GET | `/api/v1/projects/{id}/members` | A, L | List assignees + roles. |
| PUT | `/api/v1/projects/{id}/members/{userId}` | A, L | Assign role (`raAuthor`/`raReviewer`) to a user. |

### 11.4 M3 — CTD templates

**Template catalog** (SDD §4.3, ADR-013, ADR-014). Templates are country/region/global; a project pins one active template per Module (M1–M5).

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/ctd-templates` | ∀ | List templates (filter by country, module, status). |
| GET | `/api/v1/ctd-templates/{id}` | ∀ | Read template metadata. |
| POST | `/api/v1/ctd-templates` | A | Upload new template (small: multipart; large: SAS + register). |
| POST | `/api/v1/ctd-templates/{id}:archive` | A | Archive a template version. |
| GET | `/api/v1/ctd-templates/resolve` | ∀ | Query: `?projectId=&country=&module=` — returns effective template per priority chain. |
| GET | `/api/v1/ctd-templates/{id}/sections` | ∀ | Full section tree parsed from the template document. |

### 11.5 M4 — Document source configuration

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/projects/{id}/sources` | ∀ | List configured sources grouped by module. |
| POST | `/api/v1/projects/{id}/sources` | L | Add a source (SharePoint / Blob / share) — idempotent. |
| PATCH | `/api/v1/projects/{id}/sources/{sourceId}` | L | Update path/credentials. |
| POST | `/api/v1/projects/{id}/sources/{sourceId}:test` | L | Attempt connectivity + list top-level items. |
| DELETE | `/api/v1/projects/{id}/sources/{sourceId}` | L | Remove source. |

### 11.6 M5 — Discovery & OCR

Discovery is asynchronous; requests return a **run** resource that the client polls or subscribes to for progress (SSE optional).

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/projects/{id}/discovery` | L | Start a discovery run (idempotent) — enqueues to Service Bus (SDD §8). |
| GET | `/api/v1/runs/{runId}` | ∀ | Get run status + counts. |
| GET | `/api/v1/runs/{runId}/events` | ∀ | SSE stream of progress events. |
| POST | `/api/v1/runs/{runId}:cancel` | L | Request cancellation. |
| GET | `/api/v1/projects/{id}/documents` | ∀ | List discovered docs (cursor pagination). |
| GET | `/api/v1/documents/{docId}` | ∀ | Document metadata + OCR status. |
| GET | `/api/v1/documents/{docId}/content` | ∀ | Redirects to short-lived Blob SAS. |

### 11.7 M6 — Classification

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/documents/{docId}/classification` | ∀ | Classification result: predicted section, confidence, signals used. |
| POST | `/api/v1/documents/{docId}/classification:override` | L, U | Manual override; records reviewer + reason. |
| POST | `/api/v1/documents/{docId}/classification:reclassify` | L | Re-run classification (e.g., after template change). |

### 11.8 M7 — CTD mapping & dossier arrangement

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/dossiers/{id}` | ∀ | Read dossier state (draft/mapped/compiled/signed). |
| POST | `/api/v1/dossiers` | L | Create dossier for a project. |
| POST | `/api/v1/dossiers/{id}/sections/{code}:assign` | L | Assign a document to a CTD section (with source status). |
| DELETE | `/api/v1/dossiers/{id}/sections/{code}/documents/{docId}` | L | Unassign. |
| GET | `/api/v1/dossiers/{id}/sections/{code}` | ∀ | Read section detail (assignments + coverage). |
| PUT | `/api/v1/dossiers/{id}/sections/{code}/content` | U | Save section draft content (rich text). If-Match required. |

### 11.9 M8 — Gap analysis

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/dossiers/{id}/gap-analysis` | L, U | Run gap analysis (returns run id). |
| GET | `/api/v1/dossiers/{id}/gap-analysis/latest` | ∀ | Latest report (missing, partial, redundant). |
| GET | `/api/v1/dossiers/{id}/gap-analysis/{reportId}` | ∀ | Historical report. |

### 11.10 M9 — Regulatory Copilot

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/copilot/threads` | ∀ | Create a chat thread (context: project, section, or dossier). Idempotent. |
| GET | `/api/v1/copilot/threads/{id}` | ∀ | Thread metadata + last N messages. |
| GET | `/api/v1/copilot/threads/{id}/messages` | ∀ | Full message history (paginated). |
| POST | `/api/v1/copilot/threads/{id}/messages` | ∀ | Ask a question — **SSE response** (see §9). |
| POST | `/api/v1/copilot/threads/{id}:archive` | ∀ | Archive thread. |

### 11.11 M10 — Reporting

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/reports/dossier-status` | ∀ | Aggregate status across projects. |
| GET | `/api/v1/reports/authoring-throughput` | L, A | Per-user velocity. |
| GET | `/api/v1/reports/copilot-usage` | A | Token spend by user/project. |
| POST | `/api/v1/reports:export` | ∀ | Async export → returns run id; download via SAS when done. |

### 11.12 M11 — Admin

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/admin/tenants/current` | A | Tenant config snapshot. |
| PATCH | `/api/v1/admin/tenants/current` | A | Update branding, feature flags. |
| GET | `/api/v1/admin/audit` | A | Audit log query (§9.4). |
| GET | `/api/v1/admin/service-status` | A | Downstream health details (SQL, AOAI, Search, Blob, Service Bus). |

### 11.13 M12 — Dossier compilation

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/dossiers/{id}/compile` | L | Compile eCTD package (async; idempotent). Returns run id. |
| GET | `/api/v1/dossiers/{id}/packages` | ∀ | List compiled package versions. |
| GET | `/api/v1/dossiers/{id}/packages/{version}` | ∀ | Package metadata + validation summary. |
| GET | `/api/v1/dossiers/{id}/packages/{version}/download` | ∀ | Short-lived Blob SAS for the eCTD .zip. |
| POST | `/api/v1/dossiers/{id}/packages/{version}/sign-off` | R | Attest & lock (idempotent). Records Entra-authenticated signature. |

## 12. OpenAPI generation

- **Source of truth** = C# controllers/minimal-API handlers + DTO records with `[Description]` and `[Required]` attributes.
- Swashbuckle emits `openapi.json` at `/api/v1/openapi.json` (Development only by default; enabled in prod via config flag).
- SPA imports **`orval`** or **`openapi-typescript-codegen`** to generate a typed client at `src/api/generated/`. Regeneration is part of the SPA build.
- Breaking-change gate — the pipeline diffs `openapi.json` against `main` and fails the build if a removed/renamed field is not versioned.

## 13. Rate limits & quotas

Applied via ASP.NET Core rate limiting middleware (fixed window + partition per user):

| Bucket | Limit | Applies to |
|---|---|---|
| Read | 100 req / 60 s / user | GETs |
| Write | 30 req / 60 s / user | POST/PATCH/PUT/DELETE |
| Copilot messages | 20 req / 60 s / user | `POST /copilot/threads/*/messages` |
| Uploads (SAS mint) | 10 req / 60 s / user | `POST /uploads/sas` |

Exceeded → **429** with `Retry-After` header.

## 14. Change log

- **0.1** — Initial extraction from SDD; catalog v1 endpoints for M1–M12; interim MSAL SPA auth documented alongside BFF-cookie target (ADR-020).
