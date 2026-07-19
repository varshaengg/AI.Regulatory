// A1 — Admin overview. Stats + recent activity from /api/v1/admin/*.
import * as React from "react";
import { AlertTriangle, FileText, Database, Flag } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { C } from "../design/tokens";
import { Chip, Card, ScreenCaption } from "../design/primitives";
import { getAdminStats, getAdminActivity } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";

function firstName(name?: string | null, username?: string | null): string {
  const full = (name ?? "").trim();
  if (full) return full.split(/\s+/)[0]!;
  const local = (username ?? "").split("@")[0] ?? "";
  if (!local) return "there";
  const part = local.split(/[._-]/)[0]!;
  return part.charAt(0).toUpperCase() + part.slice(1);
}

function formatRelative(iso: string): string {
  const diffMin = Math.max(1, Math.round((Date.now() - new Date(iso).getTime()) / 60000));
  if (diffMin < 60) return `${diffMin}m ago`;
  const hours = Math.round(diffMin / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.round(hours / 24)}d ago`;
}

export default function A1Screen() {
  const { accounts } = useMsal();
  const user = accounts[0];
  const greetingName = firstName(user?.name, user?.username);
  const tenantHint = user?.username?.split("@")[1] ?? "";

  const stats = useApi((sig) => getAdminStats(sig), []);
  const activity = useApi((sig) => getAdminActivity(sig), []);

  const statCards = React.useMemo(() => {
    if (stats.status !== "ready") return null;
    return [
      { value: stats.data.templateCount,     label: "CTD templates",       icon: <FileText size={18} />,      color: C.brand },
      { value: stats.data.countriesMapped,   label: "Countries mapped",    icon: <Flag size={18} />,          color: C.success },
      { value: stats.data.storageAccounts,   label: "Storage accounts",    icon: <Database size={18} />,      color: "#8764B8" },
      { value: stats.data.pendingSetupTasks, label: "Setup tasks pending", icon: <AlertTriangle size={18} />, color: C.warn },
    ];
  }, [stats]);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A1" persona="Admin" />
      <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Welcome back, {greetingName}</h1>
      <p style={{ fontSize: 13, color: C.text3, marginBottom: 24 }}>Contoso Pharma{tenantHint ? ` · Tenant ${tenantHint}` : ""} · West Europe</p>

      {stats.status === "error" && <ErrorBanner message={stats.error} style={{ marginBottom: 16 }} />}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 16, marginBottom: 24 }}>
        {(statCards ?? [1,2,3,4].map(i => ({ value: "—", label: "Loading…", icon: null, color: C.text3 }))).map((stat, i) => (
          <Card key={i} style={{ padding: 16 }}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 8 }}>
              <div style={{ color: stat.color }}>{stat.icon}</div>
              <span style={{ fontSize: 28, fontWeight: 700, lineHeight: 1, color: C.text1 }}>{stat.value}</span>
            </div>
            <div style={{ fontSize: 13, color: C.text3 }}>{stat.label}</div>
          </Card>
        ))}
      </div>

      <Card style={{ padding: 16 }}>
        <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1, marginBottom: 16 }}>Recent activity</h3>
        <div style={{ display: "grid", gridTemplateColumns: "auto 1fr auto", gap: "0 12px", fontSize: 11, fontWeight: 600, color: C.text3, textTransform: "uppercase", letterSpacing: "0.04em", paddingBottom: 8, borderBottom: `1px solid ${C.border1}` }}>
          <span>Template / Source</span><span>Description</span><span>Time</span>
        </div>
        {activity.status === "loading" && <div style={{ padding: 10, fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading…</div>}
        {activity.status === "error" && <div style={{ padding: 10 }}><ErrorBanner message={activity.error} /></div>}
        {activity.status === "ready" && activity.data.map((row, i, arr) => (
          <div key={row.id} style={{
            display: "grid", gridTemplateColumns: "auto 1fr auto", gap: "0 12px",
            alignItems: "center", padding: "10px 0", fontSize: 12,
            borderBottom: i < arr.length - 1 ? `1px solid ${C.border1}` : "none",
          }}>
            <Chip color="brand">{row.tag}</Chip>
            <span style={{ color: C.text2 }}>{row.description}</span>
            <span style={{ color: C.text3, whiteSpace: "nowrap" }}>{formatRelative(row.occurredAt)}</span>
          </div>
        ))}
      </Card>
    </div>
  );
}
