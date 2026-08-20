// A4 — Per-project source configuration. Loads /api/v1/projects/{id}/sources
// and lets a RA Lead add, edit, test, and remove sources (SDD §4.4, FR-009/FR-010).
import * as React from "react";
import { useNavigate } from "react-router";
import { Plus, ChevronDown, ChevronRight, Clock, X } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Breadcrumb, ScreenCaption } from "../design/primitives";
import {
  getProject, getProjectSources,
  createProjectSource, updateProjectSource, deleteProjectSource,
  testProjectSourceCandidate, testProjectSource,
} from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { ProjectSource, ConnectionTestResult } from "../api/types";

function sourceTypeIcon(type: string): string {
  return type === "SharePoint" ? "🔷" : "☁️";
}

function statusDot(s: ProjectSource["status"]) {
  return {
    ok:      { color: C.success, label: "OK" },
    warning: { color: "#8A6100", label: "Slow" },
    error:   { color: C.danger,  label: "Error" },
  }[s];
}

function formatSynced(iso: string): string {
  const d = new Date(iso);
  const today = new Date();
  const sameDay = d.toDateString() === today.toDateString();
  const yesterday = new Date(today); yesterday.setDate(today.getDate() - 1);
  const isYesterday = d.toDateString() === yesterday.toDateString();
  const time = d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", hour12: false });
  if (sameDay) return `Today ${time}`;
  if (isYesterday) return `Yesterday ${time}`;
  return d.toISOString().slice(0, 10) + " " + time;
}

type SourceType = "Azure Blob" | "SharePoint";

type FormState = {
  moduleId: string;
  editingId: number | null;
  label: string;
  path: string;
  type: SourceType;
};

const fieldStyle: React.CSSProperties = {
  padding: "6px 10px", borderRadius: 4, border: `1px solid ${C.border1}`,
  fontSize: 13, color: C.text1, backgroundColor: "white", fontFamily: "inherit",
};

export default function A4Screen() {
  const navigate = useNavigate();
  const [projectId] = React.useState<string | null>(() =>
    new URLSearchParams(window.location.search).get("projectId"));

  const [refreshKey, setRefreshKey] = React.useState(0);
  const project = useApi((sig) => projectId ? getProject(projectId, sig) : Promise.resolve(null), [projectId]);
  const modules = useApi((sig) => projectId ? getProjectSources(projectId, sig) : Promise.resolve([]), [projectId, refreshKey]);
  const [expanded, setExpanded] = React.useState<string[]>(["M3", "M4", "M5"]);
  const toggleExpand = (id: string) => setExpanded(e => e.includes(id) ? e.filter(x => x !== id) : [...e, id]);

  const [form, setForm] = React.useState<FormState | null>(null);
  const [formCandidateTest, setFormCandidateTest] = React.useState<ConnectionTestResult | null>(null);
  const [formTesting, setFormTesting] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [formError, setFormError] = React.useState<string | null>(null);
  const [rowBusyId, setRowBusyId] = React.useState<number | null>(null);
  const [rowResults, setRowResults] = React.useState<Record<number, ConnectionTestResult>>({});
  const [rowError, setRowError] = React.useState<string | null>(null);

  const projectLabel = project.status === "ready" && project.data ? project.data.name : (projectId ?? "");

  const openAdd = (moduleId: string) => {
    setForm({ moduleId, editingId: null, label: "", path: "", type: "Azure Blob" });
    setFormCandidateTest(null);
    setFormError(null);
  };
  const openEdit = (src: ProjectSource) => {
    setForm({ moduleId: src.moduleId, editingId: src.id, label: src.label, path: src.path, type: src.type as SourceType });
    setFormCandidateTest(null);
    setFormError(null);
  };
  const closeForm = () => { setForm(null); setFormCandidateTest(null); setFormError(null); };

  const runCandidateTest = async () => {
    if (!form || !projectId) return;
    setFormTesting(true);
    setFormError(null);
    try {
      const result = await testProjectSourceCandidate(projectId, { type: form.type, path: form.path });
      setFormCandidateTest(result);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Connection test failed.");
    } finally {
      setFormTesting(false);
    }
  };

  const saveForm = async () => {
    if (!form || !projectId) return;
    if (!form.label.trim() || !form.path.trim()) {
      setFormError("Label and path are required.");
      return;
    }
    setSaving(true);
    setFormError(null);
    try {
      if (form.editingId != null) {
        await updateProjectSource(projectId, form.editingId, { label: form.label, path: form.path, type: form.type });
      } else {
        await createProjectSource(projectId, { moduleId: form.moduleId, label: form.label, path: form.path, type: form.type });
      }
      setForm(null);
      setFormCandidateTest(null);
      setRefreshKey(k => k + 1);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Failed to save source.");
    } finally {
      setSaving(false);
    }
  };

  const runRowTest = async (src: ProjectSource) => {
    if (!projectId) return;
    setRowBusyId(src.id);
    setRowError(null);
    try {
      const result = await testProjectSource(projectId, src.id);
      setRowResults(r => ({ ...r, [src.id]: result }));
      setRefreshKey(k => k + 1);
    } catch (err) {
      setRowError(err instanceof Error ? err.message : "Connection test failed.");
    } finally {
      setRowBusyId(null);
    }
  };

  const removeRow = async (src: ProjectSource) => {
    if (!projectId) return;
    if (!window.confirm(`Remove source "${src.label}"?`)) return;
    setRowBusyId(src.id);
    setRowError(null);
    try {
      await deleteProjectSource(projectId, src.id);
      setRefreshKey(k => k + 1);
    } catch (err) {
      setRowError(err instanceof Error ? err.message : "Failed to remove source.");
    } finally {
      setRowBusyId(null);
    }
  };

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A4" persona="RALead" />
      <Breadcrumb items={[
        { label: "Projects", onClick: () => navigate("/screen/L2") },
        ...(projectId ? [{ label: projectLabel, onClick: () => navigate(`/screen/L3?projectId=${encodeURIComponent(projectId)}`) }] : []),
        "Sources",
      ]} />
      {!projectId && <ErrorBanner message="No project selected. Open a project from the projects list to manage its sources." style={{ marginBottom: 16 }} />}
      <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Project sources{projectLabel ? ` · ${projectLabel}` : ""}</h1>
      <p style={{ fontSize: 13, color: C.text3, marginBottom: 16 }}>Configure one or more source locations per CTD module. ARA will pull documents from all sources in order.</p>

      <div style={{ display: "flex", gap: 0, borderBottom: `1px solid ${C.border1}`, marginBottom: 20 }}>
        {["Sources", "Team", "Settings"].map((tab, i) => (
          <button key={tab} style={{
            padding: "8px 16px", fontSize: 13, border: "none", cursor: "pointer", fontFamily: "inherit",
            borderBottom: i === 0 ? `2px solid ${C.brand}` : "2px solid transparent",
            color: i === 0 ? C.brand : C.text3, fontWeight: i === 0 ? 600 : 400,
            backgroundColor: "transparent", marginBottom: -1,
          }}>{tab}</button>
        ))}
      </div>

      {modules.status === "error" && <ErrorBanner message={modules.error} style={{ marginBottom: 12 }} />}
      {rowError && <ErrorBanner message={rowError} style={{ marginBottom: 12 }} />}
      {modules.status === "loading" && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading sources…</p>}

      {modules.status === "ready" && (
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {modules.data.map((mod) => {
            const isOpen = expanded.includes(mod.moduleId);
            const hasError = mod.sources.some(s => s.status === "error");
            const hasWarning = mod.sources.some(s => s.status === "warning");
            return (
              <Card key={mod.moduleId} style={{ padding: 0, overflow: "hidden" }}>
                <div
                  onClick={() => toggleExpand(mod.moduleId)}
                  style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 16px", cursor: "pointer", backgroundColor: isOpen ? C.bg : "white", borderBottom: isOpen ? `1px solid ${C.border1}` : "none" }}
                >
                  {isOpen ? <ChevronDown size={14} color={C.text3} /> : <ChevronRight size={14} color={C.text3} />}
                  <div style={{ width: 32, height: 32, borderRadius: 6, backgroundColor: mod.color, color: "white", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>{mod.moduleId}</div>
                  <div style={{ flex: 1 }}>
                    <span style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>{mod.moduleId} — {mod.label}</span>
                    <span style={{ fontSize: 11, color: C.text3, marginLeft: 10 }}>{mod.sources.length === 0 ? "No sources configured" : `${mod.sources.length} source${mod.sources.length !== 1 ? "s" : ""}`}</span>
                  </div>
                  {hasError && <Chip color="danger">Connection error</Chip>}
                  {!hasError && hasWarning && <Chip color="warning">Degraded</Chip>}
                  {!hasError && !hasWarning && mod.sources.length > 0 && <Chip color="success">All connected</Chip>}
                  {mod.sources.length === 0 && <Chip color="warning">Not configured</Chip>}
                  <Btn variant="primary" style={{ fontSize: 12, padding: "4px 12px" }} onClick={(e: React.MouseEvent) => { e.stopPropagation(); openAdd(mod.moduleId); }}>
                    <Plus size={11} />Add source
                  </Btn>
                </div>

                {isOpen && (
                  <div>
                    {mod.sources.length === 0 ? (
                      <div style={{ padding: "20px 16px", textAlign: "center" }}>
                        <p style={{ fontSize: 12, color: C.text3, marginBottom: 10 }}>No sources configured for {mod.moduleId}. Add at least one source so ARA can pull documents for this module.</p>
                        <Btn variant="primary" onClick={() => openAdd(mod.moduleId)}><Plus size={12} />Add first source</Btn>
                      </div>
                    ) : (
                      mod.sources.map((src, si) => {
                        const st = statusDot(src.status);
                        const testResult = rowResults[src.id];
                        const busy = rowBusyId === src.id;
                        return (
                          <div key={src.id}>
                            <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "10px 16px 10px 60px", borderBottom: (si < mod.sources.length - 1 || testResult) ? `1px solid ${C.border1}` : "none", backgroundColor: "white" }}>
                              <div style={{ cursor: "grab", color: C.disabled, fontSize: 14, lineHeight: 1, flexShrink: 0 }}>⠿</div>
                              <div style={{ width: 20, height: 20, borderRadius: "50%", backgroundColor: C.bg3, color: C.text3, fontSize: 10, fontWeight: 700, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>{si + 1}</div>
                              <span style={{ fontSize: 14, flexShrink: 0 }}>{sourceTypeIcon(src.type)}</span>
                              <div style={{ flex: 1, minWidth: 0 }}>
                                <div style={{ fontSize: 12, fontWeight: 600, color: C.text1 }}>{src.label}</div>
                                <div style={{ fontSize: 11, color: C.text3, fontFamily: "monospace", marginTop: 2 }}>{src.type} · {src.path}</div>
                              </div>
                              <div style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 11, color: st.color, flexShrink: 0 }}>
                                <div style={{ width: 7, height: 7, borderRadius: "50%", backgroundColor: st.color }} />
                                {st.label}
                              </div>
                              <div style={{ fontSize: 11, color: C.text3, display: "flex", alignItems: "center", gap: 4, flexShrink: 0 }}>
                                <Clock size={10} />{formatSynced(src.syncedAt)}
                              </div>
                              <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                                <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }} disabled={busy} onClick={() => openEdit(src)}>Edit</Btn>
                                <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }} disabled={busy} onClick={() => runRowTest(src)}>{busy ? "Testing…" : "Test"}</Btn>
                                <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px", color: C.danger }} disabled={busy} onClick={() => removeRow(src)}>Remove</Btn>
                              </div>
                            </div>
                            {testResult && (
                              <div style={{
                                padding: "6px 16px 6px 60px", fontSize: 11,
                                color: testResult.success ? C.success : C.danger,
                                backgroundColor: testResult.success ? C.successTint : C.dangerTint,
                                borderBottom: si < mod.sources.length - 1 ? `1px solid ${C.border1}` : "none",
                              }}>
                                {testResult.message} ({testResult.durationMs}ms)
                              </div>
                            )}
                          </div>
                        );
                      })
                    )}
                    {mod.sources.length > 0 && (
                      <div style={{ padding: "8px 16px 8px 60px", borderTop: `1px solid ${C.border1}`, backgroundColor: C.bg }}>
                        <Btn variant="subtle" style={{ fontSize: 12 }} onClick={() => openAdd(mod.moduleId)}><Plus size={11} />Add another source</Btn>
                      </div>
                    )}
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {form && (
        <div style={{
          position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,.35)",
          display: "flex", alignItems: "center", justifyContent: "center", zIndex: 50,
        }} onClick={closeForm}>
          <Card style={{ padding: 20, width: 440, display: "flex", flexDirection: "column", gap: 14 }}>
            <div onClick={(e) => e.stopPropagation()} style={{ display: "contents" }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1 }}>
                  {form.editingId != null ? "Edit source" : "Add source"} · {form.moduleId}
                </h3>
                <button onClick={closeForm} style={{ border: "none", background: "transparent", cursor: "pointer", color: C.text3 }}><X size={16} /></button>
              </div>

              {formError && <ErrorBanner message={formError} />}

              <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Label</label>
                <input style={fieldStyle} value={form.label} onChange={(e) => setForm(f => f && { ...f, label: e.target.value })} disabled={saving} />
              </div>

              <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Type</label>
                <select style={fieldStyle} value={form.type} onChange={(e) => setForm(f => f && { ...f, type: e.target.value as SourceType })} disabled={saving}>
                  <option value="Azure Blob">Azure Blob</option>
                  <option value="SharePoint">SharePoint</option>
                </select>
              </div>

              <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Path</label>
                <input style={fieldStyle} value={form.path} placeholder="account/container/prefix" onChange={(e) => setForm(f => f && { ...f, path: e.target.value })} disabled={saving} />
              </div>

              {formCandidateTest && (
                <div style={{
                  padding: 8, borderRadius: 4, fontSize: 12,
                  color: formCandidateTest.success ? C.success : C.danger,
                  backgroundColor: formCandidateTest.success ? C.successTint : C.dangerTint,
                }}>
                  {formCandidateTest.message} ({formCandidateTest.durationMs}ms{formCandidateTest.itemsFound != null ? `, ${formCandidateTest.itemsFound} item(s)` : ""})
                </div>
              )}

              <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
                <Btn variant="secondary" disabled={formTesting || saving || !form.path.trim()} onClick={runCandidateTest}>
                  {formTesting ? "Testing…" : "Test connection"}
                </Btn>
                <div style={{ display: "flex", gap: 8 }}>
                  <Btn variant="secondary" disabled={saving} onClick={closeForm}>Cancel</Btn>
                  <Btn variant="primary" disabled={saving} onClick={saveForm}>{saving ? "Saving…" : "Save"}</Btn>
                </div>
              </div>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
