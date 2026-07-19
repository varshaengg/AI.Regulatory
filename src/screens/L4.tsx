// L4 — Module & sub-module coverage wizard step. Sub-modules loaded from API.
import * as React from "react";
import { useState } from "react";
import { Upload, ArrowRight } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Stepper, ScreenCaption } from "../design/primitives";
import { listModules, listSubModules } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";

export default function L4Screen() {
  const [sel, setSel] = useState(2);

  const modules = useApi((sig) => listModules(sig), []);
  const active = modules.status === "ready" ? modules.data[sel] : null;
  const subs = useApi(
    (sig) => (active ? listSubModules(active.id, sig) : Promise.resolve([])),
    [active?.id],
  );

  const srcColor: Record<string, "success" | "warning" | "danger"> = { Found: "success", Partial: "warning", Missing: "danger" };
  const th: React.CSSProperties = { padding: "7px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg2 };
  const td: React.CSSProperties = { padding: "7px 12px", fontSize: 12 };

  return (
    <div style={{ padding: "24px 24px 0", display: "flex", flexDirection: "column", height: "100%" }}>
      <ScreenCaption id="L4" persona="RALead" />
      <div style={{ marginBottom: 20 }}><Stepper steps={["Basics", "Modules", "Review & Launch"]} active={1} /></div>

      <div style={{ display: "flex", gap: 16, flex: 1, minHeight: 0 }}>
        {/* Module picker */}
        <div style={{ width: 220, flexShrink: 0, display: "flex", flexDirection: "column", gap: 8 }}>
          {modules.status === "loading" && <span style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading modules…</span>}
          {modules.status === "error" && <ErrorBanner message={modules.error} />}
          {modules.status === "ready" && modules.data.map((mod, i) => (
            <button key={mod.id} onClick={() => setSel(i)} style={{
              display: "flex", alignItems: "center", gap: 12, padding: 12,
              borderRadius: 6, border: `1px solid ${sel === i ? C.brand : C.border1}`,
              backgroundColor: sel === i ? C.brandTint : "white", cursor: "pointer", textAlign: "left", fontFamily: "inherit",
            }}>
              <div style={{ width: 32, height: 32, borderRadius: "50%", backgroundColor: mod.color, color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 700, fontSize: 12, flexShrink: 0 }}>{mod.id}</div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: sel === i ? C.brandPressed : C.text1, marginBottom: 4, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{mod.label}</div>
                <Chip color={mod.status === "Configured" ? "success" : "warning"}>{mod.status}</Chip>
              </div>
            </button>
          ))}
        </div>

        {/* Right pane */}
        <div style={{ flex: 1, minWidth: 0, overflowY: "auto" }}>
          <Card style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: C.text1 }}>
              {active ? `${active.id} · ${active.label} — ${subs.status === "ready" ? subs.data.length : "…"} sub-modules` : "Loading…"}
            </h2>

            <div style={{ display: "flex", alignItems: "center", gap: 12, padding: 12, borderRadius: 4, border: `1px solid ${C.border1}` }}>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 11, fontWeight: 600, color: C.text3, marginBottom: 6 }}>MODULE TEMPLATE</div>
                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <Chip color="brand">{active ? `${active.id} · ${active.label} v4.2` : "…"}</Chip>
                  <span style={{ fontSize: 11, color: C.text3 }}>Uploaded 2025-11-08 · Sara M.</span>
                </div>
              </div>
              <Btn variant="secondary"><Upload size={12} />Upload new version</Btn>
            </div>

            <div>
              <div style={{ fontSize: 11, fontWeight: 600, color: C.text3, marginBottom: 8 }}>SUB-MODULE COVERAGE</div>
              {subs.status === "error" && <ErrorBanner message={subs.error} />}
              <div style={{ border: `1px solid ${C.border1}`, borderRadius: 4, overflow: "hidden" }}>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr>
                      {["Section code","Section title","Included","Source status","Notes"].map(h => <th key={h} style={th}>{h}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {subs.status === "loading" && (
                      <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={5}>Loading sub-modules…</td></tr>
                    )}
                    {subs.status === "ready" && subs.data.length === 0 && (
                      <tr><td style={{ ...td, color: C.text3 }} colSpan={5}>No sub-module coverage defined for this module yet.</td></tr>
                    )}
                    {subs.status === "ready" && subs.data.map((row, i) => (
                      <tr key={row.code} style={{ backgroundColor: i % 2 === 0 ? "white" : C.bg }}>
                        <td style={{ ...td, fontFamily: "monospace", color: C.brand, fontSize: 11 }}>{row.code}</td>
                        <td style={{ ...td, color: C.text1 }}>{row.title}</td>
                        <td style={td}>
                          <div style={{ width: 32, height: 16, borderRadius: 8, backgroundColor: row.included ? C.brand : C.border2, display: "flex", alignItems: "center", padding: 2 }}>
                            <div style={{ width: 12, height: 12, borderRadius: "50%", backgroundColor: "white", marginLeft: row.included ? "auto" : 0 }} />
                          </div>
                        </td>
                        <td style={td}><Chip color={srcColor[row.sourceStatus] ?? "neutral"}>{row.sourceStatus}</Chip></td>
                        <td style={{ ...td, color: C.text3 }}>{row.notes ?? ""}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </Card>
        </div>
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "16px 0", borderTop: `1px solid ${C.border1}`, marginTop: 16 }}>
        <Btn variant="subtle">← Back</Btn>
        <div style={{ display: "flex", gap: 8 }}>
          <Btn variant="secondary">Save draft</Btn>
          <Btn variant="primary">Next <ArrowRight size={13} /></Btn>
        </div>
      </div>
    </div>
  );
}
