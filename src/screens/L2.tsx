// L2 — Dossier projects catalogue. Loads project list from /api/v1/projects.
import * as React from "react";
import { Search, MoreHorizontal, Plus, Grid, List } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, FSelect, Breadcrumb, ScreenCaption } from "../design/primitives";
import { listProjects } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";

const FLAG: Record<string, string> = { DE: "🇩🇪", FR: "🇫🇷", IT: "🇮🇹", ES: "🇪🇸", NL: "🇳🇱", UK: "🇬🇧", GB: "🇬🇧", US: "🇺🇸" };

function statusChip(status: string): "brand" | "warning" | "danger" | "neutral" | "success" {
  const s = status.toLowerCase();
  if (s === "blocked") return "danger";
  if (s === "reviewing") return "warning";
  if (s === "submitted") return "success";
  if (s === "in progress" || s === "active") return "brand";
  return "neutral";
}

export default function L2Screen() {
  const projects = useApi((sig) => listProjects(200, undefined, sig).then(p => p.items), []);

  const th: React.CSSProperties = { padding: "8px 12px", textAlign: "left", fontWeight: 600, fontSize: 12, color: C.text2, borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg2 };
  const td: React.CSSProperties = { padding: "9px 12px", fontSize: 12 };

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L2" persona="RALead" />
      <Breadcrumb items={["Home", "Projects"]} />
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
        <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1 }}>Dossier projects</h1>
        <Btn variant="primary"><Plus size={13} />New dossier request</Btn>
      </div>
      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 12, flexWrap: "wrap" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 10px", borderRadius: 4, border: `1px solid ${C.border1}`, backgroundColor: "white", fontSize: 13, color: C.text3 }}>
          <Search size={13} /><span>Search projects…</span>
        </div>
        {["Country · All","Product · All","Status · All","Owner · All"].map(f => <FSelect key={f} value={f} />)}
        <div style={{ marginLeft: "auto", display: "flex", gap: 4 }}>
          <button style={{ padding: 6, borderRadius: 4, backgroundColor: C.brandTint, border: "none", cursor: "pointer" }}><Grid size={14} color={C.brand} /></button>
          <button style={{ padding: 6, borderRadius: 4, backgroundColor: C.bg3, border: "none", cursor: "pointer" }}><List size={14} color={C.text3} /></button>
        </div>
      </div>

      {projects.status === "error" && <ErrorBanner message={projects.error} style={{ marginBottom: 12 }} />}

      <Card style={{ overflow: "hidden" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr>
              {["Project name","Product","Country","Modules","Owner","Progress","Status","Created",""].map(h => <th key={h} style={th}>{h}</th>)}
            </tr>
          </thead>
          <tbody>
            {projects.status === "loading" && (
              <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={9}>Loading projects…</td></tr>
            )}
            {projects.status === "ready" && projects.data.length === 0 && (
              <tr><td style={{ ...td, color: C.text3 }} colSpan={9}>No projects.</td></tr>
            )}
            {projects.status === "ready" && projects.data.map((row, i) => (
              <tr key={row.id} style={{ backgroundColor: i % 2 === 0 ? "white" : C.bg }}>
                <td style={{ ...td, color: C.brand, fontWeight: 500, cursor: "pointer" }}>{row.name}</td>
                <td style={{ ...td, color: C.text2 }}>{row.product}</td>
                <td style={td}>{FLAG[row.country] ?? ""} {row.country}</td>
                <td style={td}><div style={{ display: "flex", gap: 3, flexWrap: "wrap" }}>{row.modules.map(m => <Chip key={m} color="brand">{m}</Chip>)}</div></td>
                <td style={{ ...td, color: C.text2 }}>{row.ownerDisplayName}</td>
                <td style={{ ...td, minWidth: 100 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <div style={{ flex: 1, height: 6, borderRadius: 3, backgroundColor: C.bg3 }}>
                      <div style={{ height: "100%", borderRadius: 3, width: `${row.progressPct}%`, backgroundColor: C.brand }} />
                    </div>
                    <span style={{ fontSize: 11, color: C.text3 }}>{row.progressPct}%</span>
                  </div>
                </td>
                <td style={td}><Chip color={statusChip(row.status)}>{row.status}</Chip></td>
                <td style={{ ...td, color: C.text3 }}>{new Date(row.createdAt).toISOString().slice(0, 10)}</td>
                <td style={td}><MoreHorizontal size={14} color={C.text3} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
