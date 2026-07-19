// Auto-split from src/app/App.tsx - screen R2.
import * as React from "react";
import { CheckCircle, X, Check, Shield } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FInput, FSelect, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";

export default function R2Screen() {
  return (
    <div style={{ minHeight: "100%", display: "flex", alignItems: "center", justifyContent: "center", padding: 32, backgroundColor: C.bg2 }}>
      <div style={{ width: "100%", maxWidth: 640, display: "flex", flexDirection: "column", gap: 16 }}>
        <ScreenCaption id="R2" persona="RAReviewer" />
        <Card style={{ padding: 24, display: "flex", flexDirection: "column", gap: 20, boxShadow: "0 4px 12px rgba(0,0,0,.12)" }}>
          <div style={{ textAlign: "center" }}>
            <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 6 }}>Sign off dossier PX-102 · DE · v1</h1>
            <p style={{ fontSize: 13, color: C.text3 }}>Review and authenticate your approval for submission</p>
          </div>

          {/* Attestation */}
          <div style={{ padding: 16, borderRadius: 6, border: `1px solid ${C.border1}`, backgroundColor: C.bg }}>
            <p style={{ fontSize: 13, lineHeight: 1.7, color: C.text1, fontFamily: "Georgia, serif" }}>
              I, Dr. Anna Chen, Head of Regulatory Affairs at Contoso Pharma GmbH, hereby attest that I have reviewed the compiled CTD dossier PX-102 · Germany · Initial Submission v1.0 and confirm that the content is complete, accurate, and compliant with applicable ICH M4 CTD guidance and EU regulatory requirements.
            </p>
          </div>

          {/* Signature block */}
          <div style={{ padding: 16, borderRadius: 6, border: `1px solid ${C.border1}` }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
              {[["Name","Dr. Anna Chen"],["Title","Head of Regulatory Affairs"],["Timestamp","2025-11-13 · 15:44:02 UTC"]].map(([k,v]) => (
                <div key={k}>
                  <div style={{ fontSize: 11, color: C.text3, marginBottom: 2 }}>{k}</div>
                  <div style={{ fontSize: 13, fontWeight: 500, color: C.text1 }}>{v}</div>
                </div>
              ))}
              <div>
                <div style={{ fontSize: 11, color: C.text3, marginBottom: 4 }}>Authentication</div>
                <Chip color="success">Entra ID authenticated</Chip>
              </div>
            </div>
          </div>

          {/* Checkbox */}
          <label style={{ display: "flex", alignItems: "flex-start", gap: 12, cursor: "pointer" }}>
            <div style={{ width: 16, height: 16, marginTop: 2, borderRadius: 3, border: `2px solid ${C.brand}`, backgroundColor: C.brand, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <Check size={10} color="white" />
            </div>
            <span style={{ fontSize: 13, lineHeight: 1.6, color: C.text1 }}>
              I confirm that all reviewed sections meet the requirements of ICH M4 CTD guidance and internal SOP RA-018.
            </span>
          </label>

          <div style={{ display: "flex", alignItems: "center", justifyContent: "flex-end", gap: 8, paddingTop: 8, borderTop: `1px solid ${C.border1}` }}>
            <Btn variant="subtle">Cancel</Btn>
            <Btn variant="secondary">Save as pending</Btn>
            <Btn variant="primary" style={{ padding: "8px 20px", fontSize: 14 }}><Shield size={14} />Sign &amp; lock package</Btn>
          </div>
        </Card>

        {/* Toast preview */}
        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 16px", borderRadius: 6, border: `1px solid ${C.border1}`, backgroundColor: "white", boxShadow: "0 4px 12px rgba(0,0,0,.12)", borderLeft: `4px solid ${C.success}` }}>
            <CheckCircle size={16} color={C.success} />
            <div>
              <div style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>Package v1 signed and locked</div>
              <div style={{ fontSize: 11, color: C.text3 }}>Ready for eCTD publishing.</div>
            </div>
            <X size={14} color={C.text3} />
          </div>
        </div>
      </div>
    </div>
  );
}
