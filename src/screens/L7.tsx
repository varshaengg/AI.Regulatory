// L7 — Prepare Dossier. Starts an AI preparation run and shows progress, discovered documents and advisory findings.
import * as React from "react";
import { Play, RefreshCw, ShieldCheck } from "lucide-react";
import { useNavigate } from "react-router";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Breadcrumb, ScreenCaption } from "../design/primitives";
import {
  getDossierRun, getDossierRunDocuments, getDossierRunEvents, getDossierRunFindings,
  listDossierRuns, revalidateDossierRun, startDossierRun,
} from "../api/resources";
import { ErrorBanner } from "../api/useApi";
import { usePermissions } from "../api/usePermissions";
import type {
  PreparationDocument, PreparationFinding, PreparationRun, PreparationRunEvent,
} from "../api/types";

const th: React.CSSProperties = { padding: "8px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}` };
const td: React.CSSProperties = { padding: "8px 12px", fontSize: 12, verticalAlign: "top" };
const EVENT_COLOR: Record<string, string> = { Info: "#60CDFF", Warning: "#F7C948", Error: "#FF7676" };

function statusChip(s: PreparationRun["status"]): "brand" | "success" | "danger" | "warning" {
  return s === "Completed" ? "success" : s === "Failed" ? "danger" : s === "Running" ? "brand" : "warning";
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export default function L7Screen() {
  const navigate = useNavigate();
  const perms = usePermissions();
  const canWrite = perms.hasPermission("DossierManagement", "Write") || perms.hasPermission("DossierManagement", "Admin");
  const projectId = React.useMemo(() => new URLSearchParams(window.location.search).get("projectId"), []);

  const [runs, setRuns] = React.useState<PreparationRun[]>([]);
  const [runId, setRunId] = React.useState<string | null>(null);
  const [run, setRun] = React.useState<PreparationRun | null>(null);
  const [events, setEvents] = React.useState<PreparationRunEvent[]>([]);
  const [docs, setDocs] = React.useState<PreparationDocument[]>([]);
  const [findings, setFindings] = React.useState<PreparationFinding[]>([]);
  const [busy, setBusy] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [expanded, setExpanded] = React.useState<string | null>(null);

  const message = (err: unknown, fallback: string) => err instanceof Error ? err.message : fallback;

  const loadRun = React.useCallback(async (id: string, signal?: AbortSignal) => {
    const [r, e, d, f] = await Promise.all([
      getDossierRun(id, signal), getDossierRunEvents(id, signal),
      getDossierRunDocuments(id, signal), getDossierRunFindings(id, signal),
    ]);
    setRun(r); setEvents(e); setDocs(d); setFindings(f);
    return r;
  }, []);

  // Load run history, then select the latest run.
  React.useEffect(() => {
    if (!projectId) { setLoading(false); return; }
    const ac = new AbortController();
    listDossierRuns(projectId, ac.signal)
      .then(list => {
        const sorted = [...list].sort((a, b) => b.startedUtc.localeCompare(a.startedUtc));
        setRuns(sorted);
        setRunId(sorted[0]?.id ?? null);
      })
      .catch(err => { if (!ac.signal.aborted) setError(message(err, "Failed to load runs.")); })
      .finally(() => { if (!ac.signal.aborted) setLoading(false); });
    return () => ac.abort();
  }, [projectId]);

  // Load the selected run and poll while it is in progress.
  React.useEffect(() => {
    if (!runId) { setRun(null); setEvents([]); setDocs([]); setFindings([]); return; }
    const ac = new AbortController();
    let timer: ReturnType<typeof setTimeout> | undefined;
    const tick = async () => {
      try {
        const r = await loadRun(runId, ac.signal);
        if (!ac.signal.aborted && (r.status === "Queued" || r.status === "Running")) timer = setTimeout(tick, 3000);
      } catch (err) {
        if (!ac.signal.aborted) setError(message(err, "Failed to load run."));
      }
    };
    tick();
    return () => { ac.abort(); if (timer) clearTimeout(timer); };
  }, [runId, loadRun]);

  const start = async () => {
    if (!projectId) return;
    setBusy(true); setError(null);
    try {
      const created = await startDossierRun(projectId);
      setRuns(rs => [created, ...rs]);
      setRunId(created.id);
    } catch (err) {
      setError(message(err, "Failed to start the run."));
    } finally { setBusy(false); }
  };

  const revalidate = async () => {
    if (!runId) return;
    setBusy(true); setError(null);
    try {
      setFindings(await revalidateDossierRun(runId));
      setEvents(await getDossierRunEvents(runId));
    } catch (err) {
      setError(message(err, "Validation failed."));
    } finally { setBusy(false); }
  };

  const findingsByDoc = React.useMemo(() => {
    const map = new Map<string, PreparationFinding[]>();
    for (const f of findings) map.set(f.documentId, [...(map.get(f.documentId) ?? []), f]);
    return map;
  }, [findings]);

  const docsByModule = React.useMemo(() => {
    const map = new Map<string, PreparationDocument[]>();
    for (const d of docs) map.set(d.moduleId, [...(map.get(d.moduleId) ?? []), d]);
    return map;
  }, [docs]);

  const ruleText = (moduleId: string, key: string) =>
    run?.snapshot.modules.find(m => m.moduleId === moduleId)?.guideline?.rules.find(r => r.ruleKey === key)?.text ?? key;

  const inProgress = run?.status === "Queued" || run?.status === "Running";

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L7" persona="RALead" />
      <Breadcrumb items={[{ label: "Projects", onClick: () => navigate("/screen/L2") }, "Prepare dossier"]} />
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 8 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Prepare dossier</h1>
          <p style={{ fontSize: 13, color: C.text3 }}>
            A run resolves the project country's templates, discovers documents from the project or default sources,
            and checks them against the module guidelines. Findings are advisory only.
          </p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          {run && (
            <Btn variant="subtle" onClick={revalidate} disabled={busy || inProgress || !canWrite}>
              <ShieldCheck size={13} />Re-run validation
            </Btn>
          )}
          <Btn variant="primary" onClick={start} disabled={busy || inProgress || !canWrite || !projectId}>
            <Play size={13} />{busy ? "Working…" : "Start run"}
          </Btn>
        </div>
      </div>

      {!projectId && <ErrorBanner message="No project selected. Open this screen from the Projects list." style={{ marginBottom: 12 }} />}
      {error && <ErrorBanner message={error} style={{ marginBottom: 12 }} />}
      {loading && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading runs…</p>}
      {!loading && projectId && runs.length === 0 && !run && (
        <p style={{ fontSize: 13, color: C.text3, fontStyle: "italic" }}>No runs yet. Start a run to prepare this dossier.</p>
      )}

      {run && (
        <>
          <div style={{ display: "flex", alignItems: "center", gap: 12, margin: "12px 0" }}>
            <Chip color={statusChip(run.status)}>{run.status}</Chip>
            <span style={{ fontSize: 12, color: C.text3 }}>
              {run.country} · started {new Date(run.startedUtc).toLocaleString()} by {run.startedBy}
            </span>
            {runs.length > 1 && (
              <select value={run.id} onChange={e => setRunId(e.target.value)}
                style={{ marginLeft: "auto", padding: "5px 8px", fontSize: 12, borderRadius: 4, border: `1px solid ${C.border1}` }}>
                {runs.map(r => <option key={r.id} value={r.id}>{new Date(r.startedUtc).toLocaleString()} · {r.status}</option>)}
              </select>
            )}
            {inProgress && <RefreshCw size={14} color={C.text3} />}
          </div>

          <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 16 }}>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              {run.snapshot.modules.map(mod => {
                const moduleDocs = docsByModule.get(mod.moduleId) ?? [];
                const flagged = moduleDocs.reduce((n, d) => n + (findingsByDoc.get(d.id) ?? []).filter(f => f.verdict === "flag").length, 0);
                return (
                  <Card key={mod.moduleId} style={{ overflow: "hidden" }}>
                    <div style={{ padding: "10px 14px", borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg2 }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <strong style={{ fontSize: 13, color: C.text1 }}>{mod.moduleId} · {mod.label}</strong>
                        <span style={{ fontSize: 12, color: C.text3 }}>{moduleDocs.length} document{moduleDocs.length === 1 ? "" : "s"}</span>
                        {flagged > 0 && <Chip color="danger">{flagged} flagged</Chip>}
                        {mod.guideline && <Chip color="brand">Guideline v{mod.guideline.version}</Chip>}
                      </div>
                      <div style={{ fontSize: 11, color: C.text3, marginTop: 4 }}>
                        Template: {mod.template ? `${mod.template.fileName} v${mod.template.version} (${mod.template.scope})` : "none"}
                        {" · "}Sources: {mod.sources.length ? mod.sources.map(s => s.label + (s.isDefault ? " (default)" : "")).join(", ") : "none"}
                      </div>
                      {mod.warnings.map((w, i) => <div key={i} style={{ fontSize: 11, color: C.warn, marginTop: 2 }}>⚠ {w}</div>)}
                    </div>
                    {moduleDocs.length > 0 && (
                      <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead><tr>{["Document", "Source", "Size", "Findings"].map(h => <th key={h} style={th}>{h}</th>)}</tr></thead>
                        <tbody>
                          {moduleDocs.map(d => {
                            const fs = findingsByDoc.get(d.id) ?? [];
                            const flags = fs.filter(f => f.verdict === "flag").length;
                            const unclear = fs.filter(f => f.verdict === "unclear").length;
                            const passes = fs.filter(f => f.verdict === "pass").length;
                            return (
                              <React.Fragment key={d.id}>
                                <tr style={{ borderBottom: `1px solid ${C.border1}`, cursor: fs.length ? "pointer" : undefined }}
                                  onClick={() => fs.length && setExpanded(expanded === d.id ? null : d.id)}>
                                  <td style={td}>{d.fileName}</td>
                                  <td style={td}>{d.sourceLabel}</td>
                                  <td style={td}>{formatSize(d.sizeBytes)}</td>
                                  <td style={td}>
                                    {fs.length === 0 ? <span style={{ color: C.text3 }}>Not validated</span> : (
                                      <span style={{ display: "inline-flex", gap: 6 }}>
                                        {flags > 0 && <Chip color="danger">{flags} flag</Chip>}
                                        {unclear > 0 && <Chip color="warning">{unclear} unclear</Chip>}
                                        {passes > 0 && <Chip color="success">{passes} pass</Chip>}
                                      </span>
                                    )}
                                  </td>
                                </tr>
                                {expanded === d.id && fs.map(f => (
                                  <tr key={f.ruleKey} style={{ backgroundColor: C.bg }}>
                                    <td style={{ ...td, paddingLeft: 28 }} colSpan={2}>
                                      <strong>{f.ruleKey}</strong> — {ruleText(d.moduleId, f.ruleKey)}
                                    </td>
                                    <td style={td}>{f.verdict}</td>
                                    <td style={{ ...td, color: C.text3 }}>{f.evidence ?? ""}</td>
                                  </tr>
                                ))}
                              </React.Fragment>
                            );
                          })}
                        </tbody>
                      </table>
                    )}
                  </Card>
                );
              })}
            </div>

            <Card style={{ overflow: "hidden", alignSelf: "start" }}>
              <div style={{ padding: "10px 14px", borderBottom: `1px solid ${C.border1}`, fontSize: 13, fontWeight: 600, color: C.text1 }}>Run log</div>
              <div style={{ backgroundColor: "#1E1E1E", padding: 12, maxHeight: 420, overflowY: "auto", fontFamily: "monospace", fontSize: 11 }}>
                {events.length === 0 && <div style={{ color: "#999" }}>No events yet.</div>}
                {events.map((e, i) => (
                  <div key={i} style={{ color: "#DDD", marginBottom: 4 }}>
                    <span style={{ color: "#888" }}>{new Date(e.occurredUtc).toLocaleTimeString()} </span>
                    <span style={{ color: EVENT_COLOR[e.severity] ?? "#DDD" }}>[{e.stage}] </span>{e.message}
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
