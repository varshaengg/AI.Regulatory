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
