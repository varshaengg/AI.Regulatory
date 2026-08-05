// Auto-split from src/app/App.tsx - screen L5.
import * as React from "react";
import { AlertTriangle } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";
import { getProject, getProjectSources } from "../api/resources";
import { useApi, ErrorBanner, LoadingLabel } from "../api/useApi";
import type { ProjectDetail, ProjectSourcesByModule } from "../api/types";

function countryLabel(code: string): string {
  const flags: Record<string, string> = { DE: "🇩🇪", FR: "🇫🇷", IT: "🇮🇹", ES: "🇪🇸", NL: "🇳🇱", GB: "🇬🇧", UK: "🇬🇧", US: "🇺🇸" };
  return `${flags[code] ?? ""} ${code}`.trim();
}

function getProjectId(): string | null {
  return new URLSearchParams(window.location.search).get("projectId");
}

function sourceSummary(group: ProjectSourcesByModule): string {
  if (group.sources.length === 0) return "No sources";
  const first = group.sources[0];
  return `${group.sources.length} source${group.sources.length === 1 ? "" : "s"} · ${first.path}`;
}

export default function L5Screen() {
  const [projectId] = React.useState<string | null>(getProjectId());
  const project = useApi<ProjectDetail | null>(
    (sig) => (projectId ? getProject(projectId, sig) : Promise.resolve(null)),
    [projectId],
  );
  const sources = useApi<ProjectSourcesByModule[]>(
    (sig) => (projectId ? getProjectSources(projectId, sig) : Promise.resolve([])),
    [projectId],
  );

  const basics = project.status === "ready" && project.data
    ? [
        ["Project name", project.data.name],
        ["Product", project.data.product],
        ["Target country", countryLabel(project.data.country)],
        ["Status", project.data.status],
        ["Updated", new Date(project.data.updatedAt).toLocaleString()],
        ["Owner", `${project.data.ownerDisplayName} (${project.data.ownerEmail})`],
      ]
    : [];

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L5" persona="RALead" />
      <div style={{ marginBottom: 24 }}><Stepper steps={["Basics", "Modules", "Review & Launch"]} active={2} /></div>

      <div style={{ marginBottom: 16, display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
        {project.status === "loading" && <LoadingLabel>Loading project details…</LoadingLabel>}
        {sources.status === "loading" && <LoadingLabel>Loading project sources…</LoadingLabel>}
        {project.status === "error" && <ErrorBanner message={project.error} />}
        {sources.status === "error" && <ErrorBanner message={sources.error} />}
        {project.status === "ready" && project.data && <Chip color="brand">Loaded from API · {project.data.id}</Chip>}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 280px", gap: 16 }}>
        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Basics</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            {project.status === "ready" && project.data ? (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                {basics.map(([k, v]) => (
                  <div key={k}>
                    <div style={{ fontSize: 11, color: C.text3 }}>{k}</div>
                    <div style={{ fontSize: 13, fontWeight: 500, color: C.text1 }}>{v}</div>
                  </div>
                ))}
              </div>
            ) : (
              <LoadingLabel>Waiting for project data…</LoadingLabel>
            )}
          </Card>

          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Modules & Sources</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["Module","Sources","Example path","Sections"].map((h) => <th key={h} style={{ paddingBottom: 8, textAlign: "left", color: C.text3, fontWeight: 600, borderBottom: `1px solid ${C.border1}`, paddingRight: 12 }}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {sources.status === "loading" && (
                  <tr><td style={{ padding: "8px 12px 8px 0", color: C.text3, fontStyle: "italic" }} colSpan={4}>Loading sources…</td></tr>
                )}
                {sources.status === "ready" && sources.data.length === 0 && (
                  <tr><td style={{ padding: "8px 12px 8px 0", color: C.text3 }} colSpan={4}>No sources configured for this project yet.</td></tr>
                )}
                {sources.status === "ready" && sources.data.map((group, i) => (
                  <tr key={group.moduleId} style={{ borderBottom: i < sources.data.length - 1 ? `1px solid ${C.border1}` : "none" }}>
                    <td style={{ padding: "8px 12px 8px 0", color: C.text1, fontWeight: 500 }}>{group.moduleId} · {group.label}</td>
                    <td style={{ padding: "8px 12px 8px 0", color: C.text2 }}>{group.sources.length}</td>
                    <td style={{ padding: "8px 12px 8px 0", color: C.text2 }}>{sourceSummary(group)}</td>
                    <td style={{ padding: "8px 12px 8px 0", color: C.text2 }}>{project.status === "ready" && project.data ? project.data.modules.includes(group.moduleId) ? "Included" : "Pending" : "…"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Assignments</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            {[{ mod: "M1–M2", name: "Priya Kapoor", init: "PK" }, { mod: "M3 Quality", name: "James Wu", init: "JW" }, { mod: "M4 Nonclinical", name: "Aisha Kone", init: "AK" }, { mod: "M5 Clinical", name: "Priya Kapoor", init: "PK" }].map((a, i) => (
              <div key={i} style={{ display: "flex", alignItems: "center", gap: 10, padding: "6px 0", borderBottom: i < 3 ? `1px solid ${C.border1}` : "none" }}>
                <div style={{ width: 24, height: 24, borderRadius: "50%", backgroundColor: C.brandTint, color: C.brandPressed, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10, fontWeight: 600 }}>{a.init}</div>
                <span style={{ fontSize: 12, color: C.text3 }}>{a.mod}</span>
                <span style={{ fontSize: 12, fontWeight: 500, color: C.text1 }}>{a.name}</span>
              </div>
            ))}
          </Card>

          <div style={{ padding: "8px 12px", borderRadius: 4, fontSize: 12, display: "flex", alignItems: "center", gap: 8, backgroundColor: C.warnTint, color: "#8A6100" }}>
            <AlertTriangle size={13} />
            <span>3 sections in M4 have no source and will be skipped. Upload content files to include them.</span>
          </div>
        </div>

        <Card style={{ padding: 16 }}>
          <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1, marginBottom: 16 }}>Run estimate</h3>
          {project.status === "ready" && project.data ? (
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
              <div>
                <div style={{ fontSize: 11, color: C.text3 }}>Estimated progress</div>
                <div style={{ fontSize: 18, fontWeight: 600, color: C.text1 }}>{project.data.progressPct}%</div>
              </div>
              <div>
                <div style={{ fontSize: 11, color: C.text3 }}>Modules loaded</div>
                <div style={{ fontSize: 18, fontWeight: 600, color: C.text1 }}>{project.data.modules.length}</div>
              </div>
              <div>
                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 6, fontSize: 12 }}>
                  <span style={{ color: C.text3 }}>Readiness</span>
                  <span style={{ color: C.text2 }}>{project.data.progressPct}%</span>
                </div>
                <div style={{ height: 8, borderRadius: 4, backgroundColor: C.bg3 }}>
                  <div style={{ height: "100%", borderRadius: 4, width: `${project.data.progressPct}%`, backgroundColor: C.brand }} />
                </div>
              </div>
            </div>
          ) : (
            <LoadingLabel>Waiting for project details…</LoadingLabel>
          )}
        </Card>
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginTop: 24 }}>
        <Btn variant="subtle">← Back</Btn>
        <div style={{ display: "flex", gap: 8 }}>
          <Btn variant="subtle">Cancel</Btn>
          <Btn variant="secondary">Save as draft</Btn>
          <Btn variant="primary" style={{ padding: "8px 20px", fontSize: 14 }}>🚀 Launch dossier run</Btn>
        </div>
      </div>
    </div>
  );
}
