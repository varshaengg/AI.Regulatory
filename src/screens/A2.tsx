// A2 — CTD template catalog. Loads templates from /api/v1/templates.
import * as React from "react";
import { Search, Trash2, Upload } from "lucide-react";
import { useNavigate } from "react-router";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FSelect, Breadcrumb, ScreenCaption } from "../design/primitives";
import { deleteGlobalTemplate, listTemplates } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { CtdTemplate } from "../api/types";

const FLAG: Record<string, string> = { DE: "🇩🇪", FR: "🇫🇷", IT: "🇮🇹", ES: "🇪🇸", NL: "🇳🇱", UK: "🇬🇧", GB: "🇬🇧", US: "🇺🇸" };

function displayCountry(country: string): string {
  return FLAG[country] ? `${FLAG[country]} ${country}` : country;
}

export default function A2Screen() {
  const navigate = useNavigate();
  const [refreshKey, setRefreshKey] = React.useState(0);
  const tmpl = useApi((sig) => listTemplates(sig).then(p => p.items), [refreshKey]);
  const [busyId, setBusyId] = React.useState<string | null>(null);
  const [actionError, setActionError] = React.useState<string | null>(null);

  const removeTemplate = async (row: CtdTemplate) => {
    const module = row.moduleId || row.modules.map(m => `M${m}`).join(", ");
    const confirmed = window.confirm(
      `Delete global template "${row.fileName || module}" (${module})? ` +
      `Projects without an override will have no default template for ${module}.`,
    );
    if (!confirmed) return;

    setBusyId(row.id);
    setActionError(null);
    try {
      await deleteGlobalTemplate(row.id);
      setRefreshKey(k => k + 1);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to delete template.");
    } finally {
      setBusyId(null);
    }
  };

  const statusColor: Record<CtdTemplate["status"], "success" | "warning" | "disabled"> = {
    Active: "success",
    Draft: "warning",
    Archived: "disabled",
  };
  const mColors: Record<string, string> = { "1": C.brand, "2": "#5C2E91", "3": C.success, "4": C.warn, "5": C.danger };
  const th: React.CSSProperties = { padding: "8px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}` };
  const td: React.CSSProperties = { padding: "10px 12px", fontSize: 12, verticalAlign: "top" };

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A2" persona="Admin" />
      <Breadcrumb items={["Admin", "CTD Templates"]} />
      <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 8 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>CTD Template Catalog</h1>
          <p style={{ fontSize: 13, color: C.text3 }}>Admin-uploaded PDF templates are global defaults per CTD module. Project teams can override them in the dossier module step.</p>
        </div>
        <Btn variant="primary" onClick={() => navigate("/screen/A3")}><Upload size={13} />Upload PDF template</Btn>
      </div>

      <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 16, marginBottom: 12, flexWrap: "wrap" }}>
        <div style={{
          display: "flex", alignItems: "center", gap: 6, padding: "5px 10px",
          borderRadius: 4, border: `1px solid ${C.border1}`, backgroundColor: "white",
          fontSize: 13, color: C.text3, flex: 1, maxWidth: 280,
        }}>
          <Search size={13} /><span>Search by country, module, version</span>
        </div>
        <FSelect value="Country · All" />
        <FSelect value="Module · All" />
        <FSelect value="Status · Active" />
        <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 6, fontSize: 12, color: C.text3 }}>
          <span>Show archived</span>
          <div style={{ width: 32, height: 16, borderRadius: 8, backgroundColor: C.border2 }} />
        </div>
      </div>

      {tmpl.status === "error" && <ErrorBanner message={tmpl.error} style={{ marginBottom: 12 }} />}
      {actionError && <ErrorBanner message={actionError} style={{ marginBottom: 12 }} />}

      <Card style={{ overflow: "hidden" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ backgroundColor: C.bg2 }}>
              {["Scope", "Module", "Template PDF", "Version", "Uploaded by", "Uploaded on", "Status", "Actions"].map(h => (
                <th key={h} style={th}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {tmpl.status === "loading" && (
              <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={8}>Loading templates…</td></tr>
            )}
            {tmpl.status === "ready" && tmpl.data.length === 0 && (
              <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={8}>No global templates uploaded yet.</td></tr>
            )}
            {tmpl.status === "ready" && tmpl.data.map((row, i) => (
              <tr key={row.id} style={{ backgroundColor: i % 2 === 0 ? "white" : C.bg }}>
                <td style={{ ...td, color: C.text1, fontWeight: 500, whiteSpace: "nowrap" }}>{displayCountry(row.country)}</td>
                <td style={{ ...td, color: C.text2 }}>{row.moduleId || row.modules.map(m => `M${m}`).join(", ")}</td>
                <td style={td}>
                  <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                    {row.modules.map(m => (
                      <div key={m} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ width: 20, height: 20, borderRadius: "50%", backgroundColor: mColors[m], color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10, fontWeight: 700, flexShrink: 0 }}>M{m}</div>
                        <span style={{ color: C.text2 }}>{row.fileName || "Template file"}</span>
                      </div>
                    ))}
                  </div>
                </td>
                <td style={{ ...td, color: C.text2 }}>v{row.version}</td>
                <td style={{ ...td, color: C.text2 }}>{row.uploadedBy}</td>
                <td style={{ ...td, color: C.text3 }}>{new Date(row.uploadedOn).toISOString().slice(0, 10)}</td>
                <td style={td}><Chip color={statusColor[row.status]}>{row.status}</Chip></td>
                <td style={td}>
                  <Btn
                    variant="subtle"
                    style={{ fontSize: 11, padding: "3px 8px", color: C.danger }}
                    disabled={busyId !== null}
                    onClick={() => removeTemplate(row)}
                    data-id={`delete-template-${row.id}`}
                    aria-label={`Delete ${row.fileName || row.moduleId || "template"}`}
                  >
                    <Trash2 size={11} />{busyId === row.id ? "Deleting…" : "Delete"}
                  </Btn>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
