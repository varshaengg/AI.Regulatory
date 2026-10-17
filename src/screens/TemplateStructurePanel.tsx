// Admin review of a template's parsed section hierarchy (parse with AI, edit, approve).
import * as React from "react";
import { Check, Plus, Save, Sparkles, Trash2, X } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Card, Chip } from "../design/primitives";
import { getTemplateStructure, parseTemplate, saveTemplateStructure } from "../api/resources";
import { ErrorBanner } from "../api/useApi";
import type { CtdTemplate, TemplateNode, TemplateStructure } from "../api/types";

const input: React.CSSProperties = { padding: "5px 8px", borderRadius: 4, border: `1px solid ${C.border1}`, fontSize: 12 };
const th: React.CSSProperties = { padding: "8px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}` };
const td: React.CSSProperties = { padding: "6px 12px", fontSize: 12, verticalAlign: "top" };

export function TemplateStructurePanel({ template, canAdmin, onClose }: {
  template: CtdTemplate; canAdmin: boolean; onClose: () => void;
}) {
  const [structure, setStructure] = React.useState<TemplateStructure | null>(null);
  const [nodes, setNodes] = React.useState<TemplateNode[]>([]);
  const [dirty, setDirty] = React.useState(false);
  const [busy, setBusy] = React.useState(false);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const apply = (s: TemplateStructure | null) => {
    setStructure(s);
    setNodes(s ? s.nodes.map(n => ({ ...n })) : []);
    setDirty(false);
  };

  // A 404 means the template has not been parsed yet.
  React.useEffect(() => {
    const ac = new AbortController();
    setLoading(true); setError(null);
    getTemplateStructure(template.id, ac.signal)
      .then(apply)
      .catch(() => { if (!ac.signal.aborted) apply(null); })
      .finally(() => { if (!ac.signal.aborted) setLoading(false); });
    return () => ac.abort();
  }, [template.id]);

  const run = async (fn: () => Promise<TemplateStructure>) => {
    setBusy(true); setError(null);
    try { apply(await fn()); }
    catch (err) { setError(err instanceof Error ? err.message : String(err)); }
    finally { setBusy(false); }
  };

  const parse = () => {
    if (dirty && !window.confirm("Re-parsing replaces the draft and discards your unsaved edits. Continue?")) return;
    if (structure?.status === "Approved" && !window.confirm("Re-parsing resets the approval. Continue?")) return;
    run(() => parseTemplate(template.id));
  };

  const update = (i: number, patch: Partial<TemplateNode>) => {
    setNodes(ns => ns.map((n, idx) => idx === i ? { ...n, ...patch } : n));
    setDirty(true);
  };
  const add = () => {
    setNodes(ns => [...ns, { nodeKey: `NEW.${ns.length + 1}`, parentKey: null, title: "", guidance: null, isMandatory: false }]);
    setDirty(true);
  };
  const remove = (i: number) => {
    setNodes(ns => {
      const key = ns[i].nodeKey;
      return ns.filter((_, idx) => idx !== i).map(n => n.parentKey === key ? { ...n, parentKey: ns[i].parentKey } : n);
    });
    setDirty(true);
  };

  const depth = (n: TemplateNode): number => {
    let d = 0;
    let p = n.parentKey;
    while (p && d < 10) { p = nodes.find(x => x.nodeKey === p)?.parentKey; d++; }
    return d;
  };

  return (
    <Card style={{ overflow: "hidden", marginTop: 16 }}>
      <div style={{ padding: "12px 16px", borderBottom: `1px solid ${C.border1}`, display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12 }}>
        <div>
          <div style={{ fontSize: 14, fontWeight: 600, color: C.text1, display: "flex", alignItems: "center", gap: 8 }}>
            Structure — {template.fileName || template.moduleId} v{template.version}
            {structure && <Chip color={structure.status === "Approved" ? "success" : "warning"}>{structure.status}</Chip>}
          </div>
          {structure?.note && <div style={{ fontSize: 12, color: C.text3, marginTop: 2 }}>{structure.note}</div>}
          {structure && <div style={{ fontSize: 11, color: C.text3, marginTop: 2 }}>Last updated by {structure.updatedBy} on {new Date(structure.updatedOn).toLocaleString()}</div>}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          {canAdmin && <Btn variant="subtle" disabled={busy} onClick={parse}><Sparkles size={13} />{structure ? "Re-parse PDF" : "Parse PDF"}</Btn>}
          {canAdmin && structure && <Btn variant="subtle" disabled={busy} onClick={add}><Plus size={13} />Add section</Btn>}
          {canAdmin && structure && <Btn variant="subtle" disabled={busy || !dirty} onClick={() => run(() => saveTemplateStructure(template.id, nodes, false))}><Save size={13} />Save draft</Btn>}
          {canAdmin && structure && <Btn variant="primary" disabled={busy || nodes.length === 0} onClick={() => run(() => saveTemplateStructure(template.id, nodes, true))}><Check size={13} />Approve</Btn>}
          <Btn variant="subtle" onClick={onClose}><X size={13} /></Btn>
        </div>
      </div>
      {error && <div style={{ padding: 12 }}><ErrorBanner message={error} /></div>}
      {loading && <div style={{ padding: 12, fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading structure…</div>}
      {!loading && !structure && (
        <div style={{ padding: 12, fontSize: 12, color: C.text3, fontStyle: "italic" }}>
          This template has not been parsed yet.{canAdmin ? " Select “Parse PDF” to propose its section hierarchy for review." : ""}
        </div>
      )}
      {structure && (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ backgroundColor: C.bg2 }}>
              {["Key", "Parent", "Title", "Guidance", "Mandatory", ""].map(h => <th key={h} style={th}>{h}</th>)}
            </tr>
          </thead>
          <tbody>
            {nodes.length === 0 && <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={6}>No sections.</td></tr>}
            {nodes.map((n, i) => (
              <tr key={i} style={{ borderBottom: `1px solid ${C.border1}` }}>
                <td style={{ ...td, paddingLeft: 12 + depth(n) * 14 }}>
                  <input style={{ ...input, width: 100 }} value={n.nodeKey} disabled={!canAdmin} onChange={e => update(i, { nodeKey: e.target.value })} />
                </td>
                <td style={td}>
                  <input style={{ ...input, width: 90 }} value={n.parentKey ?? ""} disabled={!canAdmin} onChange={e => update(i, { parentKey: e.target.value || null })} />
                </td>
                <td style={{ ...td, width: "30%" }}>
                  <input style={{ ...input, width: "100%" }} value={n.title} disabled={!canAdmin} onChange={e => update(i, { title: e.target.value })} />
                </td>
                <td style={{ ...td, width: "35%" }}>
                  <textarea style={{ ...input, width: "100%", minHeight: 36 }} value={n.guidance ?? ""} disabled={!canAdmin} onChange={e => update(i, { guidance: e.target.value || null })} />
                </td>
                <td style={td}>
                  <input type="checkbox" checked={n.isMandatory} disabled={!canAdmin} onChange={e => update(i, { isMandatory: e.target.checked })} />
                </td>
                <td style={td}>
                  {canAdmin && <button type="button" onClick={() => remove(i)} style={{ background: "none", border: "none", cursor: "pointer", color: C.danger }}><Trash2 size={13} /></button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  );
}
