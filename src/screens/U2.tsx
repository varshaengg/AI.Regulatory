// Auto-split from src/app/App.tsx - screen U2.
import * as React from "react";
import { Send, Paperclip, FileText, Zap } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FInput, FSelect, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";

export default function U2Screen() {
  return (
    <div style={{ padding: 16, height: "100%", display: "flex", flexDirection: "column" }}>
      <ScreenCaption id="U2" persona="RAAuthor" />
      <div style={{ flex: 1, display: "flex", gap: 16, minHeight: 0 }}>
        <Card style={{ flex: 1, padding: 24, overflowY: "auto", minHeight: 500 }}>
          <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: "0.06em", color: C.text3, marginBottom: 12, textTransform: "uppercase" }}>Active section · 3.2.S.4.2</div>
          <h2 style={{ fontSize: 16, fontWeight: 600, color: C.text1, marginBottom: 12, fontFamily: "Georgia, serif" }}>Analytical Procedures</h2>
          <p style={{ fontSize: 13, lineHeight: 1.7, color: C.text2, marginBottom: 16, fontFamily: "Georgia, serif" }}>
            The analytical procedures employed for characterisation and testing of Elmiravir drug substance have been developed in accordance with ICH Q2(R2) guidelines. HPLC with UV detection at 254 nm was used as the primary method for purity determination.
          </p>
          <p style={{ fontSize: 13, lineHeight: 1.7, color: C.text2, fontFamily: "Georgia, serif" }}>
            <mark style={{ backgroundColor: "#FFF3A3", textDecoration: "underline", textDecorationColor: C.warn, padding: "0 2px", borderRadius: 2 }}>
              The limit of detection (LOD) was calculated as 3.3σ/S where σ represents the standard deviation.
            </mark>{" "}
            All reagents were of analytical grade.
          </p>
        </Card>

        <Card style={{ width: 480, flexShrink: 0, display: "flex", flexDirection: "column", overflow: "hidden", minHeight: 500 }}>
          <div style={{ padding: "12px 16px", borderBottom: `1px solid ${C.border1}`, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <Zap size={14} color={C.brand} />
              <span style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>Copilot</span>
            </div>
            <Chip color="brand">gpt-4o</Chip>
          </div>
          <div style={{ flex: 1, overflowY: "auto", padding: 16, display: "flex", flexDirection: "column", gap: 16 }}>
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <div style={{ maxWidth: 300, padding: "8px 12px", borderRadius: "8px 8px 2px 8px", fontSize: 12, backgroundColor: C.brandTint, color: C.brandPressed }}>
                Find the ICH Q2(R2) reference for the LOD formula.
              </div>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <div style={{ padding: "12px 14px", borderRadius: "2px 8px 8px 8px", backgroundColor: "white", border: `1px solid ${C.border1}`, fontSize: 12 }}>
                <p style={{ lineHeight: 1.6, marginBottom: 12 }}>I found the relevant passage in ICH Q2(R2) Section 4.4.1 (Limit of Detection). The updated guidance specifies that for chromatographic methods the LOD should be expressed as LOD = 3.3 × (σ/S) with explicit reference to the noise-based calculation method.</p>
                <div style={{ borderRadius: 6, border: `1px solid ${C.border1}`, overflow: "hidden" }}>
                  <div style={{ height: 56, backgroundColor: C.bg3, display: "flex", alignItems: "center", justifyContent: "center" }}>
                    <FileText size={20} color={C.text3} />
                  </div>
                  <div style={{ padding: "8px 12px" }}>
                    <div style={{ fontSize: 12, fontWeight: 600, color: C.text1 }}>ICH_Q2_R2_Guideline_2022.pdf</div>
                    <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 4 }}>
                      <span style={{ fontSize: 11, color: C.text3 }}>Page 23 · §4.4.1</span>
                      <span style={{ fontSize: 10, padding: "1px 6px", borderRadius: 999, backgroundColor: C.successTint, color: C.success }}>94% confidence</span>
                    </div>
                    <button style={{ marginTop: 6, fontSize: 11, color: C.brand, background: "none", border: "none", cursor: "pointer" }}>Open source →</button>
                  </div>
                </div>
              </div>
              <div style={{ padding: "12px 14px", borderRadius: "2px 8px 8px 8px", backgroundColor: "white", border: `1px solid ${C.border1}`, fontSize: 12 }}>
                <p style={{ lineHeight: 1.6, marginBottom: 10 }}>Suggested rewrite:</p>
                <blockquote style={{ padding: "8px 12px", borderRadius: 4, backgroundColor: C.bg, color: C.text1, borderLeft: `3px solid ${C.brand}`, fontSize: 12, fontStyle: "italic", lineHeight: 1.6 }}>
                  "The limit of detection (LOD) was determined using the signal-to-noise approach as specified in ICH Q2(R2) §4.4.1: LOD = 3.3 × (σ/S), where σ is the standard deviation of the response and S is the slope of the calibration curve."
                </blockquote>
              </div>
            </div>
          </div>
          <div style={{ padding: 12, borderTop: `1px solid ${C.border1}` }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "7px 10px", borderRadius: 4, border: `1px solid ${C.border1}`, backgroundColor: "white", marginBottom: 6 }}>
              <Paperclip size={13} color={C.text3} />
              <input style={{ flex: 1, fontSize: 12, outline: "none", border: "none", color: C.text1, fontFamily: "inherit" }} placeholder="Ask about this section…" readOnly />
              <Send size={13} color={C.brand} />
            </div>
            <div style={{ fontSize: 10, color: C.text3, display: "flex", gap: 6, flexWrap: "wrap" }}>
              {["/rewrite","/shorten","/translate","/find-citation"].map(cmd => (
                <span key={cmd} style={{ fontFamily: "monospace", padding: "1px 5px", borderRadius: 3, backgroundColor: C.bg3 }}>{cmd}</span>
              ))}
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

