// L1 — RA Lead landing screen. Live-loads projects + notifications from the API.
import * as React from "react";
import { CheckCircle, AlertTriangle, XCircle, MoreHorizontal, FileText, Plus, MessageSquare } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, ScreenCaption } from "../design/primitives";
import { listProjects, listNotifications } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { UserNotification } from "../api/types";

const FLAG: Record<string, string> = { DE: "🇩🇪", FR: "🇫🇷", IT: "🇮🇹", ES: "🇪🇸", NL: "🇳🇱", UK: "🇬🇧", GB: "🇬🇧", US: "🇺🇸" };

function statusColor(status: string): "brand" | "warning" | "danger" | "success" | "neutral" {
  const s = status.toLowerCase();
  if (s === "blocked") return "danger";
  if (s === "reviewing") return "warning";
  if (s === "active" || s === "in progress" || s === "drafting") return "brand";
  if (s === "complete" || s === "signed" || s === "submitted") return "success";
  return "neutral";
}

function formatUpdated(iso: string): string {
  const then = new Date(iso).getTime();
  const diffMin = Math.max(1, Math.round((Date.now() - then) / 60000));
  if (diffMin < 60) return `${diffMin}m ago`;
  const hours = Math.round(diffMin / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.round(hours / 24)}d ago`;
}

const NOTIF_ICON: Record<UserNotification["kind"], React.ReactNode> = {
  success: <CheckCircle size={14} color={C.success} />,
  warning: <AlertTriangle size={14} color={C.warn} />,
  error:   <XCircle size={14} color={C.danger} />,
  info:    <FileText size={14} color={C.brand} />,
  mention: <MessageSquare size={14} color="#8764B8" />,
};

export default function L1Screen() {
  const th: React.CSSProperties = { paddingBottom: 8, textAlign: "left", fontSize: 11, fontWeight: 600, color: C.text3, borderBottom: `1px solid ${C.border1}`, paddingRight: 12 };
  const td: React.CSSProperties = { padding: "10px 12px 10px 0", fontSize: 12 };

  const projects = useApi((s) => listProjects(50, undefined, s).then(p => p.items), []);
  const notifs   = useApi((s) => listNotifications(s), []);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L1" persona="RALead" />
      <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 24 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Good morning, Marcus</h1>
          <p style={{ fontSize: 13, color: C.text3 }}>
            {projects.status === "ready"
              ? `${projects.data.length} project${projects.data.length === 1 ? "" : "s"} loaded from API`
              : projects.status === "error"
                ? "Unable to reach API"
                : "Loading projects…"}
          </p>
        </div>
        <Btn variant="primary"><Plus size={13} />New dossier request</Btn>
      </div>

      <div style={{ display: "flex", gap: 16 }}>
        <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 16 }}>
          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1, marginBottom: 12 }}>My active runs</h3>

            {projects.status === "error" && <ErrorBanner message={projects.error} style={{ marginBottom: 12 }} />}

            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr>
                  {["Project","Country","Status","Updated",""].map(h => <th key={h} style={th}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {projects.status === "loading" && (
                  <tr><td style={{ ...td, color: C.text3, fontStyle: "italic" }} colSpan={5}>Loading…</td></tr>
                )}
                {projects.status === "ready" && projects.data.length === 0 && (
                  <tr><td style={{ ...td, color: C.text3 }} colSpan={5}>No projects yet.</td></tr>
                )}
                {projects.status === "ready" && projects.data.map((row) => (
                  <tr key={row.id} style={{ borderBottom: `1px solid ${C.border1}` }}>
                    <td style={td}><span style={{ color: C.brand, fontWeight: 500, cursor: "pointer" }}>{row.name}</span></td>
                    <td style={td}>{FLAG[row.country] ?? ""} {row.country}</td>
                    <td style={td}><Chip color={statusColor(row.status)}>{row.status}</Chip></td>
                    <td style={{ ...td, color: C.text3 }}>{formatUpdated(row.createdAt)}</td>
                    <td style={td}><MoreHorizontal size={14} color={C.text3} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1, marginBottom: 12 }}>My assignments</h3>
            <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
              {[
                { label: "Review 12 sections", color: C.danger, bg: C.dangerTint },
                { label: "Approve 4 modules", color: "#8A6100", bg: C.warnTint },
                { label: "Sign off 2 packages", color: C.brand, bg: C.brandTint },
                { label: "3 comments pending", color: C.text2, bg: C.bg3 },
              ].map((a, i) => (
                <div key={i} style={{ padding: "8px 14px", borderRadius: 6, fontSize: 12, fontWeight: 600, cursor: "pointer", backgroundColor: a.bg, color: a.color }}>{a.label}</div>
              ))}
            </div>
          </Card>
        </div>

        <div style={{ width: 270, flexShrink: 0 }}>
          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1, marginBottom: 12 }}>Notifications</h3>
            {notifs.status === "loading" && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading…</p>}
            {notifs.status === "error" && <ErrorBanner message={notifs.error} />}
            {notifs.status === "ready" && notifs.data.map((n, i, arr) => (
              <div key={n.id} style={{ display: "flex", gap: 8, padding: "8px 0", borderBottom: i < arr.length - 1 ? `1px solid ${C.border1}` : "none" }}>
                <div style={{ marginTop: 2, flexShrink: 0 }}>{NOTIF_ICON[n.kind]}</div>
                <div>
                  <p style={{ fontSize: 12, color: C.text2, lineHeight: 1.4 }}>{n.text}</p>
                  <p style={{ fontSize: 11, color: C.text3, marginTop: 2 }}>{formatUpdated(n.createdAt)}</p>
                </div>
              </div>
            ))}
          </Card>
        </div>
      </div>
    </div>
  );
}
