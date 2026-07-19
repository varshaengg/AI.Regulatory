// Auto-split from src/app/App.tsx - screen A3.
import * as React from "react";
import { Upload, X } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FInput, FSelect, Stepper, Breadcrumb, ScreenCaption } from "../design/primitives";

export default function A3Screen() {
  return (
    <div style={{ position: "relative", height: "100%", minHeight: 600 }}>
      <div style={{ position: "absolute", inset: 0, padding: 24, opacity: 0.25 }}>
        <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1 }}>CTD Template Catalog</h1>
        <p style={{ fontSize: 13, color: C.text3, marginTop: 4 }}>Each CTD module has its own template file. Upload, replace, or archive templates per module.</p>
      </div>
      <div style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center", justifyContent: "center", backgroundColor: "rgba(0,0,0,0.32)" }}>
        <div style={{ width: 520, borderRadius: 8, backgroundColor: "white", border: `1px solid ${C.border1}`, boxShadow: "0 8px 32px rgba(0,0,0,.22)", overflow: "hidden" }}>
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "14px 20px", borderBottom: `1px solid ${C.border1}` }}>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: C.text1 }}>Upload CTD template</h2>
            <button style={{ color: C.text3, background: "none", border: "none", cursor: "pointer" }}><X size={18} /></button>
          </div>
          <div style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
            <ScreenCaption id="A3" persona="Admin" />
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
              <FSelect label="Country" value="Germany (DE)" />
              <FInput label="Region (auto-filled)" placeholder="Europe" />
            </div>
            <FSelect label="Module" value="M3 — Quality" />
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
              <FInput label="Version" placeholder="e.g. 4.2" />
              <FInput label="Effective date" placeholder="Pick date…" />
            </div>
            <div style={{ borderRadius: 6, border: `2px dashed ${C.border2}`, padding: "24px 16px", textAlign: "center" }}>
              <Upload size={24} color={C.text3} style={{ margin: "0 auto 8px" }} />
              <p style={{ fontSize: 13, color: C.text2 }}>
                Drag <strong>.docx</strong> here or <span style={{ fontWeight: 600, color: C.brand, cursor: "pointer" }}>Browse</span>
              </p>
              <p style={{ fontSize: 11, color: C.text3, marginTop: 4 }}>Max 25 MB</p>
            </div>
            <div style={{ padding: "8px 12px", borderRadius: 4, fontSize: 12, display: "flex", alignItems: "flex-start", gap: 8, backgroundColor: C.brandTint, color: C.brandPressed }}>
              <span style={{ marginTop: 1 }}>ℹ</span>
              <span>Uploading will replace the current active template for the selected module. Previous versions are archived automatically.</span>
            </div>
          </div>
          <div style={{ display: "flex", alignItems: "center", justifyContent: "flex-end", gap: 8, padding: "12px 20px", borderTop: `1px solid ${C.border1}`, backgroundColor: C.bg }}>
            <Btn variant="subtle">Cancel</Btn>
            <Btn variant="primary"><Upload size={13} />Upload</Btn>
          </div>
        </div>
      </div>
    </div>
  );
}

