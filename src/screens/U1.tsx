// U1 — Author section editor. Assignments loaded from /api/v1/me/assignments.
import * as React from "react";
import { Send, Edit2, MessageSquare, X, Check, Filter, RefreshCw, Link, Zap } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, ScreenCaption } from "../design/primitives";
import { listAssignments } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";

const CONF_COLOR: Record<string, string> = { Low: C.danger, Med: C.warn, High: C.success };

export default function U1Screen() {
  const assignments = useApi((sig) => listAssignments(sig), []);
  const [selected, setSelected] = React.useState<string | null>(null);

  const currentIndex = React.useMemo(() => {
    if (assignments.status !== "ready" || assignments.data.length === 0) return 0;
    const idx = assignments.data.findIndex(a => a.sectionCode === selected);
    return idx >= 0 ? idx : 0;
  }, [assignments, selected]);

  return (
    <div style={{ padding: 16, height: "100%", display: "flex", flexDirection: "column" }}>
      <ScreenCaption id="U1" persona="RAAuthor" />
      <div style={{
        flex: 1, display: "flex", borderRadius: 6, border: `1px solid ${C.border1}`,
        boxShadow: "0 4px 12px rgba(0,0,0,.12)", overflow: "hidden", minHeight: 560,
      }}>
        {/* Assignments col */}
        <div style={{ width: 280, flexShrink: 0, borderRight: `1px solid ${C.border1}`, display: "flex", flexDirection: "column" }}>
          <div style={{ padding: "12px", borderBottom: `1px solid ${C.border1}`, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <div>
              <div style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>
                {assignments.status === "ready" ? `${assignments.data.length} assignments` : "…"}
              </div>
              <div style={{ fontSize: 11, color: C.danger }}>
                {assignments.status === "ready"
                  ? `${assignments.data.filter(a => a.flagged).length} need attention`
                  : ""}
              </div>
            </div>
            <Filter size={13} color={C.text3} />
          </div>
          <div style={{ flex: 1, overflowY: "auto" }}>
            {assignments.status === "loading" && <div style={{ padding: 12, fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading…</div>}
            {assignments.status === "error" && <div style={{ padding: 12 }}><ErrorBanner message={assignments.error} /></div>}
            {assignments.status === "ready" && assignments.data.map((a, i) => {
              const active = i === currentIndex;
              return (
                <div key={a.sectionCode}
                     onClick={() => setSelected(a.sectionCode)}
                     style={{ padding: "12px", borderBottom: `1px solid ${C.border1}`, cursor: "pointer", backgroundColor: active ? C.brandTint : "white" }}>
                  <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 4 }}>
                    <span style={{ fontFamily: "monospace", fontSize: 11, fontWeight: 600, color: C.brand }}>{a.sectionCode}</span>
                    {a.flagged && <span style={{ fontSize: 10, padding: "1px 6px", borderRadius: 999, backgroundColor: C.dangerTint, color: C.danger }}>Flagged</span>}
                  </div>
                  <div style={{ fontSize: 12, fontWeight: 500, color: C.text1, marginBottom: 4 }}>{a.title}</div>
                  <div style={{ fontSize: 11, color: C.text3, marginBottom: 6 }}>{a.projectContext}</div>
                  <div style={{ display: "flex", gap: 6 }}>
                    <span style={{ fontSize: 10, padding: "1px 6px", borderRadius: 999, backgroundColor: CONF_COLOR[a.confidence] + "20", color: CONF_COLOR[a.confidence] }}>{a.confidence} confidence</span>
                    <span style={{ fontSize: 10, padding: "1px 6px", borderRadius: 999, backgroundColor: C.bg3, color: C.text3 }}>{a.citationsCount} citations</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Editor col — draft body kept mocked (dynamic section content is out-of-scope for this iteration) */}
        <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
          <div style={{ padding: "8px 16px", borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg, display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
            <Btn variant="primary" style={{ padding: "4px 10px", fontSize: 12 }}><Check size={11} />Accept</Btn>
            <Btn variant="secondary" style={{ padding: "4px 10px", fontSize: 12 }}><X size={11} />Reject</Btn>
            <Btn variant="secondary" style={{ padding: "4px 10px", fontSize: 12 }}><RefreshCw size={11} />Revise</Btn>
            <Btn variant="subtle" style={{ padding: "4px 10px", fontSize: 12 }}><MessageSquare size={11} />Comment</Btn>
            <div style={{ marginLeft: "auto", display: "flex", borderRadius: 4, border: `1px solid ${C.border1}`, overflow: "hidden" }}>
              {["Draft","Compare with source","History"].map((tab, i) => (
                <button key={tab} style={{ padding: "4px 10px", fontSize: 11, border: "none", cursor: "pointer", fontFamily: "inherit", backgroundColor: i === 0 ? C.brand : "white", color: i === 0 ? "white" : C.text3 }}>{tab}</button>
              ))}
            </div>
          </div>
          <div style={{ flex: 1, overflowY: "auto", padding: "24px 32px", backgroundColor: "#FDFCFB" }}>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: C.text1, marginBottom: 12, fontFamily: "Georgia, 'Times New Roman', serif" }}>
              {assignments.status === "ready" && assignments.data[currentIndex]
                ? `${assignments.data[currentIndex].sectionCode} ${assignments.data[currentIndex].title}`
                : "Loading…"}
            </h2>
            <p style={{ fontSize: 13, lineHeight: 1.7, color: C.text1, marginBottom: 16, fontFamily: "Georgia, 'Times New Roman', serif" }}>
              The analytical procedures employed for characterisation and testing of Elmiravir drug substance have been developed in accordance with ICH Q2(R2) guidelines. High-performance liquid chromatography (HPLC) with UV detection at 254 nm was used as the primary method for purity determination.<sup style={{ color: C.brand, fontFamily: "inherit", cursor: "pointer", fontWeight: 600, fontSize: 10 }}>¹</sup>
            </p>
            <p style={{ fontSize: 13, lineHeight: 1.7, color: C.text1, marginBottom: 16, fontFamily: "Georgia, 'Times New Roman', serif" }}>
              <mark style={{ backgroundColor: "#FFF3A3", textDecoration: "underline", textDecorationColor: C.warn, padding: "0 2px", borderRadius: 2 }}>
                The limit of detection (LOD) was calculated as 3.3σ/S where σ represents the standard deviation of the response and S is the slope of the calibration curve.
              </mark>
              <sup style={{ color: C.brand, fontFamily: "inherit", cursor: "pointer", fontWeight: 600, fontSize: 10 }}>²</sup>
            </p>
          </div>
        </div>

        {/* Copilot col — kept mocked; will move to /api/v1/copilot in the next iteration */}
        <div style={{ width: 340, flexShrink: 0, borderLeft: `1px solid ${C.border1}`, backgroundColor: C.bg, display: "flex", flexDirection: "column" }}>
          <div style={{ padding: "12px 16px", borderBottom: `1px solid ${C.border1}`, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <Zap size={14} color={C.brand} />
              <span style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>Copilot</span>
            </div>
            <Chip color="brand">gpt-4o</Chip>
          </div>
          <div style={{ flex: 1, overflowY: "auto", padding: 12, display: "flex", flexDirection: "column", gap: 12 }}>
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <div style={{ maxWidth: 240, padding: "8px 12px", borderRadius: "8px 8px 2px 8px", fontSize: 12, backgroundColor: C.brandTint, color: C.brandPressed }}>
                Explain the flagged sentence about the LOD calculation.
              </div>
            </div>
            <div style={{ padding: "10px 12px", borderRadius: "2px 8px 8px 8px", backgroundColor: "white", border: `1px solid ${C.border1}`, fontSize: 12 }}>
              <p style={{ lineHeight: 1.6, marginBottom: 10 }}>The flagged sentence uses the formula LOD = 3.3σ/S, which is the classical ICH Q2(R1) approach. Consider aligning with ICH Q2(R2) §4.4.1.</p>
              <div style={{ display: "flex", gap: 8 }}>
                <Btn variant="primary" style={{ padding: "4px 10px", fontSize: 11 }}><Edit2 size={11} />Rewrite</Btn>
                <Btn variant="secondary" style={{ padding: "4px 10px", fontSize: 11 }}><Link size={11} />Insert reference</Btn>
              </div>
            </div>
          </div>
          <div style={{ padding: 12, borderTop: `1px solid ${C.border1}` }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "7px 10px", borderRadius: 4, border: `1px solid ${C.border1}`, backgroundColor: "white" }}>
              <input style={{ flex: 1, fontSize: 12, outline: "none", border: "none", color: C.text1, fontFamily: "inherit" }} placeholder="Ask about this section…" readOnly />
              <Send size={13} color={C.brand} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
