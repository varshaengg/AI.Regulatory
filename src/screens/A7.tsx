// A7 — Global (tenant) default source configuration. Admin sets one default
// source per CTD module; RA Leads can override it per-project from A4.
import * as React from "react";
import { Pencil, Trash2, Clock, X, ShieldCheck } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, ScreenCaption } from "../design/primitives";
import {
  listGlobalSources, upsertGlobalSource, deleteGlobalSource,
  testGlobalSourceCandidate, testGlobalSource,
} from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import { usePermissions } from "../api/usePermissions";
import type { GlobalSourceModuleEntry, ConnectionTestResult } from "../api/types";

function sourceTypeIcon(type: string): string {
  return type === "SharePoint" ? "🔷" : "☁️";
}

function statusDot(s: "ok" | "warning" | "error") {
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
  label: string;
  path: string;
  type: SourceType;
};

const fieldStyle: React.CSSProperties = {
  padding: "6px 10px", borderRadius: 4, border: `1px solid ${C.border1}`,
  fontSize: 13, color: C.text1, backgroundColor: "white", fontFamily: "inherit",
};

export default function A7Screen() {
  const perms = usePermissions();
  const canWrite = perms.hasPermission("GlobalSources", "Admin");

  const [refreshKey, setRefreshKey] = React.useState(0);
  const modules = useApi((sig) => listGlobalSources(sig), [refreshKey]);

  const [form, setForm] = React.useState<FormState | null>(null);
  const [formCandidateTest, setFormCandidateTest] = React.useState<ConnectionTestResult | null>(null);
  const [formTesting, setFormTesting] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [formError, setFormError] = React.useState<string | null>(null);
  const [rowBusyId, setRowBusyId] = React.useState<string | null>(null);
  const [rowResults, setRowResults] = React.useState<Record<string, ConnectionTestResult>>({});
  const [rowError, setRowError] = React.useState<string | null>(null);

  const openSet = (entry: GlobalSourceModuleEntry) => {
    setForm({
      moduleId: entry.moduleId,
      label: entry.source?.label ?? `${entry.label} tenant default`,
      path: entry.source?.path ?? "",
      type: (entry.source?.type as SourceType) ?? "Azure Blob",
    });
    setFormCandidateTest(null);
    setFormError(null);
  };
  const closeForm = () => { setForm(null); setFormCandidateTest(null); setFormError(null); };

  const runCandidateTest = async () => {
    if (!form) return;
    setFormTesting(true);
    setFormError(null);
    try {
      const result = await testGlobalSourceCandidate({ type: form.type, path: form.path });
      setFormCandidateTest(result);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Connection test failed.");
    } finally {
      setFormTesting(false);
    }
  };

  const saveForm = async () => {
    if (!form) return;
    if (!form.label.trim() || !form.path.trim()) {
      setFormError("Label and path are required.");
      return;
    }
    setSaving(true);
    setFormError(null);
    try {
      await upsertGlobalSource(form.moduleId, { label: form.label, path: form.path, type: form.type });
      setForm(null);
      setFormCandidateTest(null);
      setRefreshKey(k => k + 1);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Failed to save default source.");
    } finally {
      setSaving(false);
    }
  };

  const runRowTest = async (moduleId: string) => {
    setRowBusyId(moduleId);
    setRowError(null);
    try {
      const result = await testGlobalSource(moduleId);
      setRowResults(r => ({ ...r, [moduleId]: result }));
      setRefreshKey(k => k + 1);
    } catch (err) {
      setRowError(err instanceof Error ? err.message : "Connection test failed.");
    } finally {
      setRowBusyId(null);
    }
  };

  const removeRow = async (entry: GlobalSourceModuleEntry) => {
    if (!window.confirm(`Remove the tenant default source for ${entry.moduleId}?`)) return;
    setRowBusyId(entry.moduleId);
    setRowError(null);
    try {
      await deleteGlobalSource(entry.moduleId);
      setRefreshKey(k => k + 1);
    } catch (err) {
      setRowError(err instanceof Error ? err.message : "Failed to remove default source.");
    } finally {
      setRowBusyId(null);
    }
  };

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A7" persona="Admin" />
      <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Global source configuration</h1>
      <p style={{ fontSize: 13, color: C.text3, marginBottom: 16 }}>
        Set the tenant-wide default source location per CTD module. Any project without its own source
        configuration for a module (see Project sources) automatically uses this default. An RA Lead can
        override it for a specific project at any time.
      </p>

      {!canWrite && (
        <div style={{
          padding: 10, borderRadius: 4, marginBottom: 16,
          backgroundColor: C.warnTint, color: "#8A6100", fontSize: 12,
          display: "flex", alignItems: "center", gap: 8,
        }}>
          <ShieldCheck size={14} /> Read-only — you need <b>Admin</b> on Global Sources to edit these defaults.
        </div>
      )}

      {modules.status === "error" && <ErrorBanner message={modules.error} style={{ marginBottom: 12 }} />}
      {rowError && <ErrorBanner message={rowError} style={{ marginBottom: 12 }} />}
      {modules.status === "loading" && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading default sources…</p>}

      {modules.status === "ready" && (
        <Card style={{ padding: 0, overflow: "hidden" }}>
          {modules.data.map((entry, i) => {
            const testResult = rowResults[entry.moduleId];
            const busy = rowBusyId === entry.moduleId;
            const st = entry.source ? statusDot(entry.source.status) : null;
            return (
              <div key={entry.moduleId}>
                <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 16px", borderBottom: i < modules.data.length - 1 || testResult ? `1px solid ${C.border1}` : "none" }}>
                  <div style={{ width: 32, height: 32, borderRadius: 6, backgroundColor: entry.color, color: "white", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>{entry.moduleId}</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>{entry.moduleId} — {entry.label}</div>
                    {entry.source ? (
                      <div style={{ fontSize: 11, color: C.text3, fontFamily: "monospace", marginTop: 2 }}>
                        {sourceTypeIcon(entry.source.type)} {entry.source.type} · {entry.source.path}
                      </div>
                    ) : (
                      <div style={{ fontSize: 11, color: C.text3, marginTop: 2 }}>No default configured</div>
                    )}
                  </div>
                  {entry.source ? <Chip color="success">Default set</Chip> : <Chip color="warning">Not configured</Chip>}
                  {st && (
                    <div style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 11, color: st.color, flexShrink: 0 }}>
                      <div style={{ width: 7, height: 7, borderRadius: "50%", backgroundColor: st.color }} />
                      {st.label}
                    </div>
                  )}
                  {entry.source && (
                    <div style={{ fontSize: 11, color: C.text3, display: "flex", alignItems: "center", gap: 4, flexShrink: 0 }}>
                      <Clock size={10} />{formatSynced(entry.source.syncedAt)}
                    </div>
                  )}
                  <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                    <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }} disabled={!canWrite} onClick={() => openSet(entry)}>
                      <Pencil size={11} />{entry.source ? "Edit" : "Set default"}
                    </Btn>
                    {entry.source && (
                      <>
                        <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }} disabled={busy || !canWrite} onClick={() => runRowTest(entry.moduleId)}>{busy ? "Testing…" : "Test"}</Btn>
                        <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px", color: C.danger }} disabled={busy || !canWrite} onClick={() => removeRow(entry)}><Trash2 size={11} />Remove</Btn>
                      </>
                    )}
                  </div>
                </div>
                {testResult && (
                  <div style={{
                    padding: "6px 16px", fontSize: 11,
                    color: testResult.success ? C.success : C.danger,
                    backgroundColor: testResult.success ? C.successTint : C.dangerTint,
                    borderBottom: i < modules.data.length - 1 ? `1px solid ${C.border1}` : "none",
                  }}>
                    {testResult.message} ({testResult.durationMs}ms)
                  </div>
                )}
              </div>
            );
          })}
        </Card>
      )}

      {form && (
        <div style={{
          position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,.35)",
          display: "flex", alignItems: "center", justifyContent: "center", zIndex: 50,
        }} onClick={closeForm}>
          <Card style={{ padding: 20, width: 440, display: "flex", flexDirection: "column", gap: 14 }}>
            <div onClick={(e) => e.stopPropagation()} style={{ display: "contents" }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1 }}>Tenant default · {form.moduleId}</h3>
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
