// Auto-split from src/app/App.tsx - screen L3.
import * as React from "react";
import { FileText, ArrowRight } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FInput, FSelect, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";

export default function L3Screen() {
  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L3" persona="RALead" />
      <div style={{ marginBottom: 24 }}><Stepper steps={["Basics", "Modules", "Review & Launch"]} active={0} /></div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 320px", gap: 24 }}>
        <Card style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
          <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1 }}>Project details</h3>
          <FInput label="Project name" placeholder="PX-102 · Germany · Initial submission" />
          <FSelect label="Product" value="PX-102 — Elmiravir 50 mg" />
          <FInput label="Product version" placeholder="v2.1" />
          <FSelect label="Target country" value="🇩🇪 Germany (DE) — Europe region" />
          <div>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2, display: "block", marginBottom: 8 }}>Submission type</label>
            <div style={{ display: "flex", gap: 24 }}>
              {["Initial","Variation","Renewal"].map((opt, i) => (
                <label key={opt} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, cursor: "pointer", color: C.text1 }}>
                  <div style={{ width: 16, height: 16, borderRadius: "50%", border: `2px solid ${i === 0 ? C.brand : C.border2}`, display: "flex", alignItems: "center", justifyContent: "center" }}>
                    {i === 0 && <div style={{ width: 8, height: 8, borderRadius: "50%", backgroundColor: C.brand }} />}
                  </div>
                  {opt}
                </label>
              ))}
            </div>
          </div>
          <FInput label="Target submission date" placeholder="2026-03-31" />
          <FInput label="Owner" placeholder="Marcus Lindqvist (you)" />
        </Card>

        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1, marginBottom: 12 }}>Templates assigned</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
              {[
                { id: "M1", label: "Administrative", version: "4.2", color: C.brand },
                { id: "M2", label: "Summaries", version: "4.1", color: "#5C2E91" },
                { id: "M3", label: "Quality", version: "4.2", color: C.success },
                { id: "M4", label: "Nonclinical", version: "3.9", color: C.warn },
                { id: "M5", label: "Clinical", version: "4.0", color: C.danger },
              ].map((m, i, arr) => (
                <div key={m.id} style={{ display: "flex", alignItems: "center", gap: 10, padding: "7px 0", borderBottom: i < arr.length - 1 ? `1px solid ${C.border1}` : "none" }}>
                  <div style={{ width: 24, height: 24, borderRadius: "50%", backgroundColor: m.color, color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10, fontWeight: 700, flexShrink: 0 }}>{m.id}</div>
                  <span style={{ flex: 1, fontSize: 12, color: C.text1 }}>{m.label}</span>
                  <span style={{ fontSize: 11, fontFamily: "monospace", color: C.text3 }}>v{m.version}</span>
                  <FileText size={12} color={C.text3} />
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginTop: 24 }}>
        <Btn variant="subtle">Cancel</Btn>
        <Btn variant="primary">Next <ArrowRight size={13} /></Btn>
      </div>
    </div>
  );
}

