// Typed contracts mirroring api/src/AI.Regulatory.API/Contracts/Contracts.cs.
// Keep in sync manually until we generate from OpenAPI.

export interface BrandingTokens {
  productName: string;
  primaryColor: string;
  logoUrl?: string | null;
}

export interface BffConfig {
  tenantId: string;
  apiBaseUrl: string;
  featureFlags: Record<string, boolean>;
  branding: BrandingTokens;
}

export interface UserProfile {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
  avatarUrl?: string | null;
}

export interface PageInfo {
  pageSize: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface Page<T> {
  items: T[];
  pageInfo: PageInfo;
}

// ─── Projects — L1, L2, L5, L6, R1 ─────────────────────────────────────────
export interface ProjectSummary {
  id: string;
  name: string;
  country: string;
  status: string;
  product: string;
  modules: string[];
  ownerDisplayName: string;
  progressPct: number;
  createdAt: string; // ISO
}

export interface ProjectDetail extends ProjectSummary {
  ownerEmail: string;
  updatedAt: string;
  etag: string;
}

export interface CreateProjectRequest {
  name: string;
  country: string;
  product?: string;
}

// ─── Templates — A2 ─────────────────────────────────────────────────────────
export interface CtdTemplate {
  id: string;
  country: string;
  region: string;
  modules: string[];
  version: string;
  uploadedBy: string;
  uploadedOn: string;
  status: "Active" | "Draft" | "Archived";
}

// ─── Project sources — A4 ───────────────────────────────────────────────────
export interface ProjectSource {
  id: number;
  projectId: string;
  moduleId: string;
  label: string;
  path: string;
  type: "Azure Blob" | "SharePoint" | string;
  syncedAt: string;
  status: "ok" | "warning" | "error";
}

export interface ProjectSourcesByModule {
  moduleId: string;
  label: string;
  color: string;
  sources: ProjectSource[];
}

// ─── Modules & sub-modules — L4 ─────────────────────────────────────────────
export interface CtdModule {
  id: string;
  label: string;
  color: string;
  status: "Configured" | "Needs input" | string;
}

export interface SubModule {
  code: string;
  title: string;
  included: boolean;
  sourceStatus: "Found" | "Partial" | "Missing";
  notes?: string | null;
}

// ─── Author assignments — U1 ────────────────────────────────────────────────
export interface Assignment {
  sectionCode: string;
  title: string;
  projectContext: string;
  confidence: "Low" | "Med" | "High";
  citationsCount: number;
  flagged: boolean;
}

// ─── Reviewer comments — R1 ─────────────────────────────────────────────────
export interface Comment {
  id: number;
  sectionCode: string;
  author: string;
  initials: string;
  createdAt: string;
  text: string;
  resolved: boolean;
}

// ─── Dossier runs — L6 ──────────────────────────────────────────────────────
export interface RunModule {
  id: string;
  label: string;
  done: number;
  total: number;
  pct: number;
  activity: string;
  status: "ok" | "running" | "blocked";
}

export interface RunLogEntry {
  time: string;
  level: "INFO" | "OK" | "WARN" | "ERROR";
  message: string;
}

export interface RunAgent {
  name: string;
  status: "running" | "idle";
}

export interface DossierRun {
  id: string;
  projectId: string;
  status: "running" | "complete" | "blocked";
  startedAt: string;
  modules: RunModule[];
  log: RunLogEntry[];
  agents: RunAgent[];
}

// ─── Notifications — L1 ─────────────────────────────────────────────────────
export interface UserNotification {
  id: string;
  kind: "success" | "warning" | "error" | "info" | "mention";
  text: string;
  createdAt: string;
}

// ─── Document tree — R1 ─────────────────────────────────────────────────────
export interface DocTreeNode {
  code: string;
  label: string;
  moduleId?: string | null;
  color?: string | null;
  comments: number;
  active: boolean;
  children: DocTreeNode[];
}

// ─── Admin — A1 ─────────────────────────────────────────────────────────────
export interface AdminStats {
  templateCount: number;
  countriesMapped: number;
  storageAccounts: number;
  pendingSetupTasks: number;
}

export interface AdminActivity {
  id: string;
  tag: string;
  description: string;
  occurredAt: string;
}

/** RFC 9457 Problem Details envelope returned by the API on errors. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  [key: string]: unknown;
}

/** Runtime error thrown by the API client with the parsed Problem Details attached. */
export class ApiError extends Error {
  constructor(
    public status: number,
    public problem: ProblemDetails,
  ) {
    super(problem.title ?? `HTTP ${status}`);
    this.name = "ApiError";
  }
}
