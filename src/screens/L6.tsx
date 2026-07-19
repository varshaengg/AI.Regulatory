// L6 — Live dossier run. Loads run details from /api/v1/runs/{id}.
import * as React from "react";
import { Terminal } from "lucide-react";
import { C } from "../design/tokens";
import { Chip, Card, Breadcrumb, ScreenCaption } from "../design/primitives";
import { getRun } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";

const LOG_COLORS: Record<string, string> = { INFO: "#60CDFF", OK: "#6FCF97", WARN: "#F7C948", ERROR: "#FF7676" };

// Hardcoded run id for the wireframe demo — mirrors L5 launch.
const DEMO_RUN_ID = "4821";

export default function L6Screen() {
  const run = useApi((sig) => getRun(DEMO_RUN_ID, sig), []);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L6" persona="RALead" />
      <Breadcrumb items={["Projects", "PX-102 · DE · Initial"]} />
      <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 20 }}>
        <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1 }}>Dossier run #{DEMO_RUN_ID}</h1>
        {run.status === "ready" && (
          <Chip color={run.data.status === "running" ? "brand" : run.data.status === "blocked" ? "danger" : "success"}>
            {run.data.status[0].toUpperCase() + run.data.status.slice(1)} · started {new Date(run.data.startedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
          </Chip>
        )}
      </div>

      {run.status === "error" && <ErrorBanner message={run.error} style={{ marginBottom: 16 }} />}
      {run.status === "loading" && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading run…</p>}

      {run.status === "ready" && (
        <>
          <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 16, marginBottom: 16 }}>
            {/* Module progress */}
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              {run.data.modules.map(mod => {
                const bg = mod.status === "ok" ? C.success : mod.status === "blocked" ? C.danger : C.brand;
                return (
                  <Card key={mod.id} style={{ padding: 12 }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                      <div style={{ width: 32, height: 32, borderRadius: "50%", backgroundColor: bg, color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 700, fontSize: 11, flexShrink: 0 }}>{mod.id}</div>
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 4 }}>
                          <span style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>{mod.label}</span>
                          <span style={{ fontSize: 11, color: C.text3 }}>{mod.done}/{mod.total} sections</span>
                        </div>
                        <div style={{ height: 6, borderRadius: 3, backgroundColor: C.bg3, marginBottom: 4 }}>
                          <div style={{ height: "100%", borderRadius: 3, width: `${mod.pct}%`, backgroundColor: bg }} />
                        </div>
                        <div style={{ fontSize: 11, color: mod.status === "blocked" ? C.danger : C.text3 }}>{mod.activity}</div>
                      </div>
                      {mod.status === "blocked" && (
                        <button style={{ fontSize: 11, color: C.brand, background: "none", border: "none", cursor: "pointer", whiteSpace: "nowrap" }}>Assign to author</button>
                      )}
                    </div>
                  </Card>
                );
              })}
            </div>

            {/* Live log */}
            <Card style={{ overflow: "hidden" }}>
              <div style={{ padding: "8px 12px", borderBottom: "1px solid #333", backgroundColor: "#1E1E1E", display: "flex", alignItems: "center", gap: 8 }}>
                <Terminal size={13} color="#888" />
                <span style={{ fontSize: 12, color: "#888" }}>Live activity log</span>
                <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 4, fontSize: 11, color: "#6FCF97" }}>
                  <div style={{ width: 6, height: 6, borderRadius: "50%", backgroundColor: "#6FCF97" }} />
                  Auto-scroll
                </div>
              </div>
              <div style={{ padding: 12, backgroundColor: "#1A1A1A", color: "#CCC", height: 380, overflowY: "auto", fontFamily: "monospace", fontSize: 11 }}>
                {run.data.log.map((line, i) => (
                  <div key={i} style={{ display: "flex", gap: 8, marginBottom: 4 }}>
                    <span style={{ color: "#666", flexShrink: 0 }}>{line.time}</span>
                    <span style={{ color: LOG_COLORS[line.level], flexShrink: 0 }}>[{line.level}]</span>
                    <span style={{ color: line.level === "ERROR" ? "#FF7676" : line.level === "WARN" ? "#F7C948" : "#CCC" }}>{line.message}</span>
                  </div>
                ))}
                <div style={{ display: "flex", alignItems: "center", gap: 4, marginTop: 8 }}>
                  <div style={{ width: 8, height: 14, backgroundColor: "#60CDFF", animation: "pulse 1s infinite" }} />
                </div>
              </div>
            </Card>
          </div>

          {/* Agents */}
          <Card style={{ padding: 12 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12, flexWrap: "wrap" }}>
              <span style={{ fontSize: 12, fontWeight: 600, color: C.text2 }}>Agents involved:</span>
              {run.data.agents.map(agent => (
                <div key={agent.name} style={{ display: "flex", alignItems: "center", gap: 6, padding: "4px 10px", borderRadius: 999, border: `1px solid ${C.border1}`, backgroundColor: "white", fontSize: 12 }}>
                  <div style={{ width: 6, height: 6, borderRadius: "50%", backgroundColor: agent.status === "running" ? C.success : C.border2 }} />
                  <span style={{ color: C.text2 }}>{agent.name}</span>
                </div>
              ))}
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
