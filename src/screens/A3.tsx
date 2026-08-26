// A3 — Admin upload for tenant-wide default CTD template PDFs.
import * as React from "react";
import { Upload, X } from "lucide-react";
import { useNavigate } from "react-router";
import { C } from "../design/tokens";
import { Btn, Card, FSelect, Breadcrumb, ScreenCaption } from "../design/primitives";
import { uploadGlobalTemplate } from "../api/resources";

const modules = [
  { id: "M1", label: "M1 — Administrative" },
  { id: "M2", label: "M2 — Summaries" },
  { id: "M3", label: "M3 — Quality" },
  { id: "M4", label: "M4 — Nonclinical" },
  { id: "M5", label: "M5 — Clinical" },
] as const;

export default function A3Screen() {
  const navigate = useNavigate();
  const [moduleId, setModuleId] = React.useState("M3");
  const [version, setVersion] = React.useState("");
  const [file, setFile] = React.useState<File | null>(null);
  const [status, setStatus] = React.useState<{ kind: "info" | "success" | "error"; text: string } | null>(null);
  const [uploading, setUploading] = React.useState(false);

  const validPdf = !file || file.name.toLowerCase().endsWith(".pdf");
  const canUpload = Boolean(version.trim() && file && validPdf && !uploading);

  async function upload() {
    if (!file || !canUpload) return;

    setUploading(true);
    setStatus(null);
    try {
      const saved = await uploadGlobalTemplate(moduleId, version.trim(), file);
      setStatus({ kind: "success", text: `${saved.moduleId} global default updated to ${saved.fileName} (v${saved.version}).` });
      setFile(null);
    } catch (err) {
      setStatus({ kind: "error", text: err instanceof Error ? err.message : String(err) });
    } finally {
      setUploading(false);
    }
  }

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A3" persona="Admin" />
      <Breadcrumb items={[
        { label: "Admin", onClick: () => navigate("/screen/A1") },
        { label: "CTD Templates", onClick: () => navigate("/screen/A2") },
        "Upload Template",
      ]} />
      <Card style={{ maxWidth: 640, margin: "0 auto", overflow: "hidden" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "14px 20px", borderBottom: `1px solid ${C.border1}` }}>
          <div>
            <h1 style={{ fontSize: 18, fontWeight: 600, color: C.text1 }}>Upload global CTD template</h1>
            <p style={{ fontSize: 12, color: C.text3, marginTop: 4 }}>Admin-uploaded PDFs are tenant defaults. RA Leads can override them on a project module.</p>
          </div>
          <button type="button" style={{ color: C.text3, background: "none", border: "none", cursor: "pointer" }} onClick={() => navigate("/screen/A2")} data-id="close-template-upload">
            <X size={18} />
          </button>
        </div>
        <div style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
            <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              <span style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Module</span>
              <select
                value={moduleId}
                onChange={(e) => setModuleId(e.target.value)}
                style={{ padding: "7px 10px", borderRadius: 4, border: `1px solid ${C.border1}`, fontSize: 13 }}
                data-id="template-module"
              >
                {modules.map((m) => <option key={m.id} value={m.id}>{m.label}</option>)}
              </select>
            </label>
            <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              <span style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Version</span>
              <input
                value={version}
                onChange={(e) => setVersion(e.target.value)}
                placeholder="e.g. 4.2"
                style={{ padding: "7px 10px", borderRadius: 4, border: `1px solid ${C.border1}`, fontSize: 13 }}
                data-id="template-version"
              />
            </label>
          </div>

          <div style={{ borderRadius: 6, border: `2px dashed ${validPdf ? C.border2 : C.danger}`, padding: "24px 16px", textAlign: "center" }}>
            <Upload size={24} color={C.text3} style={{ margin: "0 auto 8px" }} />
            <label style={{ fontSize: 13, color: C.text2, cursor: "pointer" }}>
              Select CTD template <strong>.pdf</strong>
              <input
                type="file"
                accept="application/pdf,.pdf"
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                style={{ display: "none" }}
                data-id="template-file"
              />
            </label>
            <p style={{ fontSize: 11, color: validPdf ? C.text3 : C.danger, marginTop: 4 }}>
              {file ? file.name : "PDF only · Max 25 MB"}
            </p>
          </div>

          <div style={{ padding: "8px 12px", borderRadius: 4, fontSize: 12, display: "flex", alignItems: "flex-start", gap: 8, backgroundColor: C.brandTint, color: C.brandPressed }}>
            <span style={{ marginTop: 1 }}>i</span>
            <span>This upload replaces the current active global default for the selected module. Existing project overrides remain unchanged.</span>
          </div>

          {status && (
            <div style={{ padding: 10, borderRadius: 4, fontSize: 12, backgroundColor: status.kind === "success" ? C.successTint : C.dangerTint, color: status.kind === "success" ? C.success : C.danger }}>
              {status.text}
            </div>
          )}
        </div>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "flex-end", gap: 8, padding: "12px 20px", borderTop: `1px solid ${C.border1}`, backgroundColor: C.bg }}>
          <Btn variant="subtle" onClick={() => navigate("/screen/A2")}>Cancel</Btn>
          <Btn variant="primary" onClick={upload} disabled={!canUpload}>
            <Upload size={13} />{uploading ? "Uploading..." : "Upload PDF"}
          </Btn>
        </div>
      </Card>
    </div>
  );
}
