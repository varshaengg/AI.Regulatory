// A2 — CTD template catalog. Loads templates from /api/v1/templates.
import * as React from "react";
import { Search, Upload } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FSelect, Breadcrumb, ScreenCaption } from "../design/primitives";
import { listTemplates } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { CtdTemplate } from "../api/types";

const FLAG: Record<string, string> = { DE: "🇩🇪", FR: "🇫🇷", IT: "🇮🇹", ES: "🇪🇸", NL: "🇳🇱", UK: "🇬🇧", GB: "🇬🇧", US: "🇺🇸" };

function displayCountry(country: string): string {
  return FLAG[country] ? `${FLAG[country]} ${country}` : country;
}

export default function A2Screen() {
  const tmpl = useApi((sig) => listTemplates(sig).then(p => p.items), []);

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
          <p style={{ fontSize: 13, color: C.text3 }}>Templates apply per country. View, replace, archive, or upload the template for each module individually.</p>
        </div>
        <Btn variant="primary"><Upload size={13} />Upload template</Btn>
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

      <Card style={{ overflow: "hidden" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ backgroundColor: C.bg2 }}>
              {["Country", "Region", "Modules — per-module actions", "Version", "Uploaded by", "Uploaded on", "Status"].map(h => (
                <th key={h} style={th}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {tmpl.status === "loading" && (
              <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={7}>Loading templates…</td></tr>
            )}
            {tmpl.status === "ready" && tmpl.data.map((row, i) => (
              <tr key={row.id} style={{ backgroundColor: i % 2 === 0 ? "white" : C.bg }}>
                <td style={{ ...td, color: C.text1, fontWeight: 500, whiteSpace: "nowrap" }}>{displayCountry(row.country)}</td>
                <td style={{ ...td, color: C.text2 }}>{row.region}</td>
                <td style={td}>
                  <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                    {row.modules.map(m => (
                      <div key={m} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ width: 20, height: 20, borderRadius: "50%", backgroundColor: mColors[m], color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10, fontWeight: 700, flexShrink: 0 }}>M{m}</div>
                        <div style={{ display: "flex", gap: 6 }}>
                          {[["View", C.brand], ["Replace", C.brand], ["Archive", C.text3], ["Upload", C.brand]].map(([label, color]) => (
                            <button key={label} style={{ fontSize: 11, color, background: "none", border: "none", cursor: "pointer", fontFamily: "inherit", padding: 0 }}>{label}</button>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </td>
                <td style={{ ...td, color: C.text2 }}>v{row.version}</td>
                <td style={{ ...td, color: C.text2 }}>{row.uploadedBy}</td>
                <td style={{ ...td, color: C.text3 }}>{new Date(row.uploadedOn).toISOString().slice(0, 10)}</td>
                <td style={td}><Chip color={statusColor[row.status]}>{row.status}</Chip></td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
