// Domain-specific API helpers. Each function is a thin wrapper around `api.*`
// with the correct path and typed response so screens can use them directly.

import { api } from "./client";
import type {
  Page,
  ProjectSummary, ProjectDetail, CreateProjectRequest,
  CtdTemplate,
  ProjectSourcesByModule,
  CtdModule, SubModule,
  Assignment,
  Comment,
  DossierRun,
  UserNotification,
  DocTreeNode,
  AdminStats, AdminActivity,
  Persona, Feature, Permission,
  PermissionMatrixEntry, UpdatePermissionRequest,
  AppUser, CreateAppUserRequest, AssignPersonasRequest,
  AadPerson, MePermissions,
} from "./types";

// Projects
export const listProjects = (pageSize = 50, cursor?: string, signal?: AbortSignal) =>
  api.get<Page<ProjectSummary>>("/projects", { pageSize, cursor }, signal);
export const getProject = (id: string, signal?: AbortSignal) =>
  api.get<ProjectDetail>(`/projects/${encodeURIComponent(id)}`, undefined, signal);
export const createProject = (body: CreateProjectRequest, signal?: AbortSignal) =>
  api.post<ProjectDetail>("/projects", body, signal);

// Templates
export const listTemplates = (signal?: AbortSignal) =>
  api.get<Page<CtdTemplate>>("/templates", undefined, signal);

// Project sources
export const getProjectSources = (projectId: string, signal?: AbortSignal) =>
  api.get<ProjectSourcesByModule[]>(
    `/projects/${encodeURIComponent(projectId)}/sources`, undefined, signal);

// Modules & sub-modules
export const listModules = (signal?: AbortSignal) =>
  api.get<CtdModule[]>("/modules", undefined, signal);
export const listSubModules = (moduleId: string, signal?: AbortSignal) =>
  api.get<SubModule[]>(`/modules/${encodeURIComponent(moduleId)}/submodules`, undefined, signal);

// Author personal
export const listAssignments = (signal?: AbortSignal) =>
  api.get<Assignment[]>("/me/assignments", undefined, signal);
export const listNotifications = (signal?: AbortSignal) =>
  api.get<UserNotification[]>("/me/notifications", undefined, signal);

// Reviewer
export const listComments = (projectId: string, signal?: AbortSignal) =>
  api.get<Comment[]>(`/projects/${encodeURIComponent(projectId)}/comments`, undefined, signal);
export const getDocTree = (projectId: string, signal?: AbortSignal) =>
  api.get<DocTreeNode[]>(`/projects/${encodeURIComponent(projectId)}/doc-tree`, undefined, signal);

// Runs
export const getRun = (id: string, signal?: AbortSignal) =>
  api.get<DossierRun>(`/runs/${encodeURIComponent(id)}`, undefined, signal);

// Admin
export const getAdminStats = (signal?: AbortSignal) =>
  api.get<AdminStats>("/admin/stats", undefined, signal);
export const getAdminActivity = (signal?: AbortSignal) =>
  api.get<AdminActivity[]>("/admin/activity", undefined, signal);

// ─── User & Access Management — A5, A6 ────────────────────────────────────

// Personas
export const listPersonas = (signal?: AbortSignal) =>
  api.get<Persona[]>("/personas", undefined, signal);

// Permissions
export const listPermissionVerbs = (signal?: AbortSignal) =>
  api.get<Permission[]>("/permissions/verbs", undefined, signal);
export const listFeatures = (signal?: AbortSignal) =>
  api.get<Feature[]>("/permissions/features", undefined, signal);
export const getPermissionMatrix = (signal?: AbortSignal) =>
  api.get<PermissionMatrixEntry[]>("/permissions/matrix", undefined, signal);
export const togglePermission = (body: UpdatePermissionRequest, signal?: AbortSignal) =>
  api.put<PermissionMatrixEntry>("/permissions/matrix", body, signal);

// App users
export const listUsers = (signal?: AbortSignal) =>
  api.get<AppUser[]>("/users", undefined, signal);
export const createUser = (body: CreateAppUserRequest, signal?: AbortSignal) =>
  api.post<AppUser>("/users", body, signal);
export const assignUserPersonas = (userId: string, body: AssignPersonasRequest, signal?: AbortSignal) =>
  api.put<AppUser>(`/users/${encodeURIComponent(userId)}/personas`, body, signal);
export const deleteUser = (userId: string, signal?: AbortSignal) =>
  api.del<void>(`/users/${encodeURIComponent(userId)}`, signal);

// People picker (customer AD only, via Graph proxy)
export const searchAadPeople = (query: string, top = 10, signal?: AbortSignal) =>
  api.get<AadPerson[]>("/aad/people", { search: query, top }, signal);

// Resolve a single person by email (fallback / DB lookup when needed)
export const resolveAadPersonByEmail = (email: string, signal?: AbortSignal) =>
  api.get<AadPerson>("/aad/people/resolve", { email }, signal);

// Effective permissions for the caller (feeds usePermissions hook)
export const getMyPermissions = (signal?: AbortSignal) =>
  api.get<MePermissions>("/me/permissions", undefined, signal);
