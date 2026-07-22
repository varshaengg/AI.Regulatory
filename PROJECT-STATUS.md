# AI Regulatory Assistant — Project Status Report
**Generated:** 2026-07-22 17:12 UTC+05:30  
**Project:** Regulatory AI Assistant (CTD Module 1-5 Preparation)  
**Status:** MVP Phase — User Management Complete, Dossier Workflows in Progress

---

## 📊 Completion Summary

| Phase | Status | Progress | Details |
|-------|--------|----------|---------|
| **User Management** | ✅ DONE | 100% | A5/A6 screens, SQL persistence, people-picker (AAD OBO) |
| **Responsive UI** | ✅ DONE | 100% | Shell + nav rail, hamburger drawer (<768px) |
| **Deployment** | ✅ DONE | 100% | BYOC model, UAMI, DACPAC, GH + ADO pipelines |
| **Admin Bootstrap** | ✅ DONE | 100% | Sopan pre-configured as Admin in SQL seed |
| **Dossier Workflows** | 🟡 IN PROGRESS | ~30% | L1 loads projects; L2-L6 scaffolded |
| **AI Integration** | ⏳ PENDING | 0% | Copilot/embeddings for classification |
| **Backend API** | ⏳ PENDING | 40% | Users/Personas done; Projects/Templates partial |
| **E2E Testing** | ⏳ PENDING | 0% | Unit tests written; integration tests pending |

---

## ✅ Completed Work

### Phase 1: User Management & Access Control
- ✅ **Database Schema**: AppUser, UserPersona, Persona, Permission, Feature, PersonaPermission
- ✅ **React Screens**: A5 (user list/add/delete), A6 (permission matrix toggle)
- ✅ **Backend API**: UsersController, PermissionsController, AppUsersRepository
- ✅ **AAD Integration**: People-picker with Microsoft Graph OBO (on-behalf-of) flow
- ✅ **Seed Data**: 5 test users (Admin, RaLead, RaAuthor, RaReviewer, mixed)
- ✅ **SQL Migrations**: DACPAC deployed via Azure Pipelines

### Phase 2: Frontend Architecture
- ✅ **Responsive Shell**: AppBar + NavRail + lazy-loaded screens
- ✅ **Mobile Support**: Hamburger drawer + responsive breakpoints
- ✅ **Design Tokens**: Tailwind CSS 4 + shadcn/ui primitives
- ✅ **Auth**: MSAL (Entra ID) + RequireAuth wrapper

### Phase 3: Infrastructure & Deployment
- ✅ **BYOC Model**: Customer-owned Azure subscription, single-tenant per install
- ✅ **Managed Identity**: UAMI for API authentication (no secrets in code)
- ✅ **App Service Split**: SPA + API on separate App Services, nginx reverse proxy
- ✅ **CI/CD Pipelines**: GitHub Actions (tier-2 templates) + Azure DevOps (tier-1)
- ✅ **Configuration Management**: Per-environment VITE + app-settings sourcing
- ✅ **Health Checks**: /health/live endpoint + liveness probes

---

## 🟡 In Progress

### Dossier Preparation Workflows (L1-L6)

| Screen | Purpose | Status | Notes |
|--------|---------|--------|-------|
| **L1** | Dashboard / project list | 🟡 60% | Loads projects + notifications; needs refresh UI |
| **L2** | Template explorer | ⏳ 0% | User selects CTD template for regulatory region |
| **L3** | Source discovery | ⏳ 0% | Searches configured sources (SharePoint, Blob) |
| **L4** | Content mapping | ⏳ 0% | Assigns discovered docs to CTD modules |
| **L5** | Dossier compilation | ⏳ 0% | Generates folder tree, PDF, Word, gap report |
| **L6** | Review & sign-off | ⏳ 0% | Sign-off workflow, approval chain |

**Backend needed for L-series:**
- `GET /api/v1/projects` — list user's projects
- `POST /api/v1/projects` — create new dossier request
- `GET /api/v1/templates` — list available CTD templates
- `GET /api/v1/projects/{id}/sources` — discover sources per module
- `POST /api/v1/projects/{id}/sources/search` — search across sources
- `PUT /api/v1/projects/{id}/assignments` — slot documents into CTD structure
- `POST /api/v1/projects/{id}/compile` — trigger dossier compilation
- `GET /api/v1/runs/{id}` — poll compilation status (SSE for streaming)

---

## ⏳ Pending

### Admin Configuration Screens (A1-A4)

| Screen | Purpose | Status |
|--------|---------|--------|
| **A1** | System settings | ⏳ Empty scaffold |
| **A2** | CTD catalog management | ⏳ Empty scaffold |
| **A3** | Country → template mapping | ⏳ Empty scaffold |
| **A4** | Repository integrations (SharePoint, Blob) | ⏳ Empty scaffold |

### AI Integration
- **Document Classification**: Use Copilot API to classify documents against CTD module structure
- **Gap Detection**: Identify missing sections and recommend content sources
- **Embeddings**: Vector search in Azure AI Search for semantic discovery
- **Streaming**: SSE (Server-Sent Events) for real-time AI recommendations

### Backend Expansion
- **Projects API**: CRUD, status tracking, user-project ACL
- **Templates API**: Versioning, validation, inheritance
- **Sources API**: Multi-source aggregation, unified search
- **Runs API**: Async compilation, progress streaming, output artifacts

### Testing
- **Unit Tests**: Dapper repositories, business logic
- **Integration Tests**: End-to-end workflows
- **E2E Tests**: Playwright for React screens + API
- **Accessibility**: WCAG 2.1 audit

---

## 🔧 Recent Changes

### Commit: `097ad9a`
**"feat(sql): preconfigure Sopan as Admin in seed data"**
- Added Sopan (GUID: 1a403da5-4458-420a-adaf-6ff802800cd8) to AppUser seed
- Automatically assigns Admin persona on SQL deployment
- Idempotent MERGE + INSERT prevents duplicates on re-runs
- **Impact**: Eliminates manual bootstrap for development; ready for next deploy cycle

---

## 📋 Quick Reference

### Deployed Environment

| Component | URL / Details |
|-----------|--------------|
| **SPA** | https://[deployment-host]/spa |
| **API** | https://[deployment-host]/api/v1 |
| **SQL** | Azure SQL (AAD auth only, BYOC) |
| **Auth** | Entra ID (tenant-specific) |

### Key Features

| Feature | Implementation | Status |
|---------|----------------|--------|
| Multi-user enrollment | A5 people-picker + graph OBO | ✅ Live |
| Role-based access | Persona + permissions matrix | ✅ Live |
| Project management | L1 list view (data partial) | 🟡 In progress |
| Document discovery | Scaffolded (API pending) | ⏳ Pending |
| AI-powered classification | Not started | ⏳ Pending |
| CTD compilation | Database tables ready, UI pending | ⏳ Pending |

### Database Schema

```
AppUser (id, AadObjectId, DisplayName, Email, Upn, IsActive)
  ├─ UserPersona (UserId, PersonaId, AssignedUtc, AssignedBy)
  │   └─ Persona (id, Code: Admin|RaLead|RaAuthor|RaReviewer)
  │       └─ PersonaPermission (PersonaId, FeatureId, PermissionId)
  │           └─ Feature (id, Code: UserManagement|DossierManagement|...)
  │               └─ Permission (id, Code: Read|Write|Review|Admin)
```

---

## 🚀 Recommended Next Steps

### **Priority 1: Dossier Core Workflows (2-3 weeks)**
Build L1-L6 screens to enable end-to-end dossier preparation:
1. Implement `ProjectsRepository` (CRUD, queries)
2. Build L2 (template selection) + L3 (source discovery)
3. Wire up backend `GET /projects`, `POST /projects`, `GET /templates`
4. Add project search/filter UI to L1

### **Priority 2: AI Document Analysis (1-2 weeks)**
Integrate Copilot for smart recommendations:
1. Set up Azure Copilot integration (prompts, embeddings)
2. Implement document classification pipeline
3. Build L4 content mapping UI with AI suggestions
4. Add SSE streaming for real-time recommendations

### **Priority 3: Compliance & Testing (1-2 weeks)**
Hardening for production:
1. Write integration tests (repositories, workflows)
2. WCAG 2.1 accessibility audit + fixes
3. Security review (OWASP, managed identity, DLP)
4. Documentation (API OpenAPI, runbooks, admin guide)

---

## 📚 Key Documentation

| Document | Purpose | Location |
|----------|---------|----------|
| **SDD** | Technical design, C4 architecture, ADRs | `docs/SDD.md` |
| **API Design** | Endpoint catalog, contracts, error handling | `docs/API-Design.md` |
| **Bootstrap Guide** | Admin bootstrap procedure | `docs/BOOTSTRAP-ADMIN.md` |
| **BRD** | Business requirements, user stories | `docs/` (Word doc) |
| **Deployment Guide** | BYOC install, runbooks | `deploy/` |

---

## 💾 Repository Structure

```
AI.Regulatory/
├── src/                           (React SPA)
│   ├── screens/                   (A1-A6, L1-L6, R1-R2, U1-U2)
│   ├── api/                       (API client, types, hooks)
│   ├── auth/                      (MSAL, RequireAuth)
│   ├── layout/                    (AppBar, NavRail, responsive)
│   ├── design/                    (tokens, primitives)
│   └── styles/                    (Tailwind, theme)
├── api/src/AI.Regulatory.API/     (ASP.NET Core BFF)
│   ├── Controllers/               (Users, Permissions, Projects, etc.)
│   ├── Data/                      (Repositories, Dapper)
│   ├── Contracts/                 (DTOs)
│   └── Auth/                      (JWT validation, BFF)
├── data/sql/                      (SQL Server DACPAC)
│   └── AI.Regulatory.Sql/
│       ├── dbo/Tables/            (Schema)
│       └── Script.PostDeployment.sql (Seed)
├── deploy/                        (Bicep, pipelines, scripts)
│   ├── bicep/                     (App Services, SQL, networking)
│   ├── pipelines/                 (GitHub Actions, Azure DevOps)
│   └── scripts/                   (Bootstrap, diagnostics)
└── docs/                          (Design, runbooks, guides)
```

---

## ❓ Questions & Open Items

1. **AI Integration Strategy**: Use Azure Copilot, OpenAI, or local embeddings?
2. **Source System Priority**: Start with SharePoint, Blob, or hybrid?
3. **Compilation Output**: PDF/Word/both? What metadata to embed?
4. **Scalability**: Expected max concurrent users? Max project size?
5. **Multi-tenant**: BYOC only or future SaaS?

---

**Session Owner:** Sopan  
**Last Updated:** 2026-07-22 17:12 UTC+05:30  
**Next Review:** Post-Dossier Workflows implementation
