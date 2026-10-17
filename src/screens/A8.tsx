// A8 — Admin module guidelines. Upload a guideline per module, review AI-extracted rules, publish.
import * as React from "react";
import { Plus, Save, Trash2, Upload } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Card, Breadcrumb, ScreenCaption } from "../design/primitives";
import {
  deleteGuideline, listGuidelines, publishGuideline, saveGuidelineRules, unpublishGuideline, uploadGuideline,
} from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { GuidelineRule, ModuleGuideline } from "../api/types";
import { usePermissions } from "../api/usePermissions";

const MODULES = ["ALL", "M1", "M2", "M3", "M4", "M5"];
const COUNTRIES = ["Global", "DE", "FR", "IT", "ES", "NL", "GB"];
const SEVERITIES = ["Low", "Medium", "High"];
const input: React.CSSProperties = { padding: "6px 8px", borderRadius: 4, border: `1px solid ${C.border1}`, fontSize: 12 };
const th: React.CSSProperties = { padding: "8px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}` };
const td: React.CSSProperties = { padding: "8px 12px", fontSize: 12, verticalAlign: "top" };

export default function A8Screen() {
  const perms = usePermissions();
  const canAdmin = perms.hasPermission("Templates", "Admin");
  const [refreshKey, setRefreshKey] = React.useState(0);
  const list = useApi((sig) => listGuidelines(sig).then(p => p.items), [refreshKey]);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [rules, setRules] = React.useState<GuidelineRule[]>([]);
  const [dirty, setDirty] = React.useState(false);
  const [busy, setBusy] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [moduleId, setModuleId] = React.useState("M3");
  const [country, setCountry] = React.useState("Global");
  const [version, setVersion] = React.useState("");
  const [file, setFile] = React.useState<File | null>(null);

  const items = list.status === "ready" ? list.data : [];
  const selected = items.find(g => g.id === selectedId) ?? null;

  const select = (g: ModuleGuideline | null) => {
    setSelectedId(g?.id ?? null);
    setRules(g ? g.rules.map(r => ({ ...r })) : []);
    setDirty(false);
  };

  const run = async (fn: () => Promise<ModuleGuideline | void>, keep = true) => {
    setBusy(true);
    setError(null);
    try {
      const result = await fn();
      setRefreshKey(k => k + 1);
      if (result && keep) select(result);
      return result;
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const upload = () => file && run(async () => {
    const saved = await uploadGuideline(moduleId, version.trim(), country, file);
    setFile(null);
    setVersion("");
    return saved;
  });

  const updateRule = (i: number, patch: Partial<GuidelineRule>) => {
    setRules(rs => rs.map((r, idx) => idx === i ? { ...r, ...patch } : r));
    setDirty(true);
  };
  const addRule = () => {
    setRules(rs => [...rs, { ruleKey: `R${rs.length + 1}`, text: "", severity: "Medium", nodeRef: null }]);
    setDirty(true);
  };
  const removeRule = (i: number) => {
    setRules(rs => rs.filter((_, idx) => idx !== i));
    setDirty(true);
  };

  const canUpload = Boolean(canAdmin && version.trim() && file && !busy);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A8" persona="Admin" />
      <Breadcrumb items={["Admin", "Module Guidelines"]} />
      <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Module Guidelines</h1>
      <p style={{ fontSize: 13, color: C.text3, marginBottom: 16 }}>
        Guidelines drive AI validation of dossier documents. Upload a guideline PDF, review the extracted rules, then publish.
        Findings are advisory only. A country guideline overrides the Global one.
      </p>

      {list.status === "error" && <ErrorBanner message={list.error} style={{ marginBottom: 12 }} />}
      {error && <ErrorBanner message={error} style={{ marginBottom: 12 }} />}

      <Card style={{ padding: 16, marginBottom: 16 }}>
        <div style={{ display: "flex", gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
          <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 12, color: C.text2 }}>Country
            <select style={input} value={country} onChange={e => setCountry(e.target.value)}>
              {COUNTRIES.map(c => <option key={c}>{c}</option>)}
            </select>
          </label>
          <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 12, color: C.text2 }}>Module
            <select style={input} value={moduleId} onChange={e => setModuleId(e.target.value)}>
              {MODULES.map(m => <option key={m}>{m}</option>)}
            </select>
          </label>
          <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 12, color: C.text2 }}>Version
            <input style={input} value={version} onChange={e => setVersion(e.target.value)} placeholder="e.g. 1.0" />
          </label>
          <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 12, color: C.text2 }}>Guideline file
            <input type="file" accept=".pdf,.docx" onChange={e => setFile(e.target.files?.[0] ?? null)} style={{ fontSize: 12 }} />
          </label>
          <Btn variant="primary" onClick={upload} disabled={!canUpload}>
            <Upload size={13} />{busy ? "Working…" : "Upload & extract rules"}
          </Btn>
        </div>
      </Card>

      <Card style={{ overflow: "hidden", marginBottom: 16 }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ backgroundColor: C.bg2 }}>
              {["Country", "Module", "File", "Version", "Rules", "Status", "Actions"].map(h => <th key={h} style={th}>{h}</th>)}
            </tr>
          </thead>
          <tbody>
            {list.status === "loading" && <tr><td style={{ ...td, color: C.text3 }} colSpan={7}>Loading guidelines…</td></tr>}
            {list.status === "ready" && items.length === 0 && (
              <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={7}>No guidelines uploaded yet.</td></tr>
            )}
            {items.map(g => (
              <tr key={g.id} style={{ backgroundColor: g.id === selectedId ? C.brandTint : undefined, borderBottom: `1px solid ${C.border1}` }}>
                <td style={td}>{g.country}</td>
                <td style={td}>{g.moduleId}</td>
                <td style={td}>{g.fileName}</td>
                <td style={td}>{g.version}</td>
                <td style={td}>{g.rules.length}</td>
                <td style={td}>{g.isPublished ? "Published" : "In review"}</td>
                <td style={{ ...td, display: "flex", gap: 6 }}>
                  <Btn variant="subtle" onClick={() => select(g)}>Review rules</Btn>
                  {canAdmin && (g.isPublished
                    ? <Btn variant="subtle" disabled={busy} onClick={() => run(() => unpublishGuideline(g.id))}>Unpublish</Btn>
                    : <Btn variant="subtle" disabled={busy || g.rules.length === 0} onClick={() => run(() => publishGuideline(g.id))}>Publish</Btn>)}
                  {canAdmin && (
                    <Btn variant="subtle" disabled={busy} onClick={() => {
                      if (!window.confirm(`Delete guideline "${g.fileName}" (${g.moduleId}/${g.country})?`)) return;
                      if (g.id === selectedId) select(null);
                      run(() => deleteGuideline(g.id), false);
                    }}><Trash2 size={13} /></Btn>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      {selected && (
        <Card style={{ overflow: "hidden" }}>
          <div style={{ padding: "12px 16px", borderBottom: `1px solid ${C.border1}`, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <div style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>
                Rules — {selected.moduleId} / {selected.country} v{selected.version}
              </div>
              {selected.extractionNote && <div style={{ fontSize: 12, color: C.text3, marginTop: 2 }}>{selected.extractionNote}</div>}
              {selected.isPublished && <div style={{ fontSize: 12, color: C.warn, marginTop: 2 }}>Saving edits to a published guideline changes validation immediately.</div>}
            </div>
            {canAdmin && (
              <div style={{ display: "flex", gap: 8 }}>
                <Btn variant="subtle" onClick={addRule}><Plus size={13} />Add rule</Btn>
                <Btn variant="primary" disabled={!dirty || busy} onClick={() => run(() => saveGuidelineRules(selected.id, rules))}>
                  <Save size={13} />Save rules
                </Btn>
              </div>
            )}
          </div>
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ backgroundColor: C.bg2 }}>
                {["Key", "Rule", "Severity", "Section ref", ""].map(h => <th key={h} style={th}>{h}</th>)}
              </tr>
            </thead>
            <tbody>
              {rules.length === 0 && <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={5}>No rules. Add rules manually.</td></tr>}
              {rules.map((r, i) => (
                <tr key={i} style={{ borderBottom: `1px solid ${C.border1}` }}>
                  <td style={td}><input style={{ ...input, width: 70 }} value={r.ruleKey} disabled={!canAdmin} onChange={e => updateRule(i, { ruleKey: e.target.value })} /></td>
                  <td style={{ ...td, width: "55%" }}><textarea style={{ ...input, width: "100%", minHeight: 44 }} value={r.text} disabled={!canAdmin} onChange={e => updateRule(i, { text: e.target.value })} /></td>
                  <td style={td}>
                    <select style={input} value={r.severity} disabled={!canAdmin} onChange={e => updateRule(i, { severity: e.target.value })}>
                      {SEVERITIES.map(s => <option key={s}>{s}</option>)}
                    </select>
                  </td>
                  <td style={td}><input style={{ ...input, width: 100 }} value={r.nodeRef ?? ""} disabled={!canAdmin} onChange={e => updateRule(i, { nodeRef: e.target.value || null })} /></td>
                  <td style={td}>{canAdmin && <button type="button" onClick={() => removeRule(i)} style={{ background: "none", border: "none", cursor: "pointer", color: C.danger }}><Trash2 size={13} /></button>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}
