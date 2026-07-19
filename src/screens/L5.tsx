// Auto-split from src/app/App.tsx - screen L5.
import * as React from "react";
import { Upload, AlertTriangle } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FInput, FSelect, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";

export default function L5Screen() {
  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L5" persona="RALead" />
      <div style={{ marginBottom: 24 }}><Stepper steps={["Basics", "Modules", "Review & Launch"]} active={2} /></div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 280px", gap: 16 }}>
        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          {/* Basics */}
          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Basics</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
              {[["Project name","PX-102 · Germany · Initial submission"],["Product","PX-102 — Elmiravir 50 mg"],["Target country","🇩🇪 Germany (DE)"],["Submission type","Initial"],["Target date","2026-03-31"],["Owner","Marcus Lindqvist"]].map(([k,v]) => (
                <div key={k}>
                  <div style={{ fontSize: 11, color: C.text3 }}>{k}</div>
                  <div style={{ fontSize: 13, fontWeight: 500, color: C.text1 }}>{v}</div>
                </div>
              ))}
            </div>
          </Card>

          {/* Modules & Sources */}
          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Modules & Sources</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["Module","Template","Source path","Sections"].map(h => <th key={h} style={{ paddingBottom: 8, textAlign: "left", color: C.text3, fontWeight: 600, borderBottom: `1px solid ${C.border1}`, paddingRight: 12 }}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {[["M1 Administrative","EU v4.2","contosopharma/px102/m1","8/8"],["M2 Summaries","EU v4.2","contosopharma/px102/m2","14/14"],["M3 Quality","EU v4.2","contosopharma/px102/m3","10/12"],["M4 Nonclinical","EU v4.2","contosopharma/px102/m4","4/6"],["M5 Clinical","EU v4.2","contosopharma/px102/m5","22/22"]].map((row,i) => (
                  <tr key={i} style={{ borderBottom: i < 4 ? `1px solid ${C.border1}` : "none" }}>
                    {row.map((cell,j) => <td key={j} style={{ padding: "8px 12px 8px 0", color: j===0 ? C.text1 : C.text2, fontWeight: j===0 ? 500 : 400 }}>{cell}</td>)}
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          {/* Assignments */}
          <Card style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
              <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Assignments</h3>
              <button style={{ fontSize: 12, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Edit</button>
            </div>
            {[{ mod: "M1–M2", name: "Priya Kapoor", init: "PK" },{ mod: "M3 Quality", name: "James Wu", init: "JW" },{ mod: "M4 Nonclinical", name: "Aisha Kone", init: "AK" },{ mod: "M5 Clinical", name: "Priya Kapoor", init: "PK" }].map((a,i) => (
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

        {/* Run estimate */}
        <Card style={{ padding: 16 }}>
          <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1, marginBottom: 16 }}>Run estimate</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
            <div>
              <div style={{ fontSize: 11, color: C.text3 }}>Estimated time</div>
              <div style={{ fontSize: 18, fontWeight: 600, color: C.text1 }}>18 – 25 min</div>
            </div>
            <div>
              <div style={{ fontSize: 11, color: C.text3 }}>Sections to draft</div>
              <div style={{ fontSize: 18, fontWeight: 600, color: C.text1 }}>58 sections</div>
            </div>
            <div>
              <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 6, fontSize: 12 }}>
                <span style={{ color: C.text3 }}>Readiness</span>
                <span style={{ color: C.text2 }}>68%</span>
              </div>
              <div style={{ height: 8, borderRadius: 4, backgroundColor: C.bg3 }}>
                <div style={{ height: "100%", borderRadius: 4, width: "68%", backgroundColor: C.brand }} />
              </div>
            </div>
          </div>
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

