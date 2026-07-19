// A4 — Per-project source configuration. Loads /api/v1/projects/{id}/sources.
import * as React from "react";
import { Plus, ChevronDown, ChevronRight, Clock } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Breadcrumb, ScreenCaption } from "../design/primitives";
import { getProjectSources } from "../api/resources";
import { useApi, ErrorBanner } from "../api/useApi";
import type { ProjectSource } from "../api/types";

// Wireframe demo uses the DE variant of PX-102.
const DEMO_PROJECT_ID = "px-102-de";

function sourceTypeIcon(type: string): string {
  return type === "SharePoint" ? "🔷" : "☁️";
}

function statusDot(s: ProjectSource["status"]) {
  return {
    ok:      { color: C.success, label: "OK" },
    warning: { color: "#8A6100", label: "Slow" },
    error:   { color: C.danger,  label: "Error" },
  }[s];
}

function formatSynced(iso: string): string {
  const d = new Date(iso);
  const today = new Date();
  const sameDay = d.toDateString() === today.toDateString();
  const yesterday = new Date(today); yesterday.setDate(today.getDate() - 1);
  const isYesterday = d.toDateString() === yesterday.toDateString();
  const time = d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", hour12: false });
  if (sameDay) return `Today ${time}`;
  if (isYesterday) return `Yesterday ${time}`;
  return d.toISOString().slice(0, 10) + " " + time;
}

export default function A4Screen() {
  const modules = useApi((sig) => getProjectSources(DEMO_PROJECT_ID, sig), []);
  const [expanded, setExpanded] = React.useState<string[]>(["M3", "M4", "M5"]);
  const toggleExpand = (id: string) => setExpanded(e => e.includes(id) ? e.filter(x => x !== id) : [...e, id]);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A4" persona="RALead" />
      <Breadcrumb items={["Projects", "PX-102", "Sources"]} />
      <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, marginBottom: 4 }}>Project sources · PX-102</h1>
      <p style={{ fontSize: 13, color: C.text3, marginBottom: 16 }}>Configure one or more source locations per CTD module. ARA will pull documents from all sources in order.</p>

      <div style={{ display: "flex", gap: 0, borderBottom: `1px solid ${C.border1}`, marginBottom: 20 }}>
        {["Sources", "Team", "Settings"].map((tab, i) => (
          <button key={tab} style={{
            padding: "8px 16px", fontSize: 13, border: "none", cursor: "pointer", fontFamily: "inherit",
            borderBottom: i === 0 ? `2px solid ${C.brand}` : "2px solid transparent",
            color: i === 0 ? C.brand : C.text3, fontWeight: i === 0 ? 600 : 400,
            backgroundColor: "transparent", marginBottom: -1,
          }}>{tab}</button>
        ))}
      </div>

      {modules.status === "error" && <ErrorBanner message={modules.error} style={{ marginBottom: 12 }} />}
      {modules.status === "loading" && <p style={{ fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading sources…</p>}

      {modules.status === "ready" && (
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {modules.data.map((mod) => {
            const isOpen = expanded.includes(mod.moduleId);
            const hasError = mod.sources.some(s => s.status === "error");
            const hasWarning = mod.sources.some(s => s.status === "warning");
            return (
              <Card key={mod.moduleId} style={{ padding: 0, overflow: "hidden" }}>
                <div
                  onClick={() => toggleExpand(mod.moduleId)}
                  style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 16px", cursor: "pointer", backgroundColor: isOpen ? C.bg : "white", borderBottom: isOpen ? `1px solid ${C.border1}` : "none" }}
                >
                  {isOpen ? <ChevronDown size={14} color={C.text3} /> : <ChevronRight size={14} color={C.text3} />}
                  <div style={{ width: 32, height: 32, borderRadius: 6, backgroundColor: mod.color, color: "white", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>{mod.moduleId}</div>
                  <div style={{ flex: 1 }}>
                    <span style={{ fontSize: 13, fontWeight: 600, color: C.text1 }}>{mod.moduleId} — {mod.label}</span>
                    <span style={{ fontSize: 11, color: C.text3, marginLeft: 10 }}>{mod.sources.length === 0 ? "No sources configured" : `${mod.sources.length} source${mod.sources.length !== 1 ? "s" : ""}`}</span>
                  </div>
                  {hasError && <Chip color="danger">Connection error</Chip>}
                  {!hasError && hasWarning && <Chip color="warning">Degraded</Chip>}
                  {!hasError && !hasWarning && mod.sources.length > 0 && <Chip color="success">All connected</Chip>}
                  {mod.sources.length === 0 && <Chip color="warning">Not configured</Chip>}
                  <Btn variant="primary" style={{ fontSize: 12, padding: "4px 12px" }} onClick={(e: React.MouseEvent) => { e.stopPropagation(); }}>
                    <Plus size={11} />Add source
                  </Btn>
                </div>

                {isOpen && (
                  <div>
                    {mod.sources.length === 0 ? (
                      <div style={{ padding: "20px 16px", textAlign: "center" }}>
                        <p style={{ fontSize: 12, color: C.text3, marginBottom: 10 }}>No sources configured for {mod.moduleId}. Add at least one source so ARA can pull documents for this module.</p>
                        <Btn variant="primary"><Plus size={12} />Add first source</Btn>
                      </div>
                    ) : (
                      mod.sources.map((src, si) => {
                        const st = statusDot(src.status);
                        return (
                          <div key={src.id} style={{ display: "flex", alignItems: "center", gap: 12, padding: "10px 16px 10px 60px", borderBottom: si < mod.sources.length - 1 ? `1px solid ${C.border1}` : "none", backgroundColor: "white" }}>
                            <div style={{ cursor: "grab", color: C.disabled, fontSize: 14, lineHeight: 1, flexShrink: 0 }}>⠿</div>
                            <div style={{ width: 20, height: 20, borderRadius: "50%", backgroundColor: C.bg3, color: C.text3, fontSize: 10, fontWeight: 700, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>{si + 1}</div>
                            <span style={{ fontSize: 14, flexShrink: 0 }}>{sourceTypeIcon(src.type)}</span>
                            <div style={{ flex: 1, minWidth: 0 }}>
                              <div style={{ fontSize: 12, fontWeight: 600, color: C.text1 }}>{src.label}</div>
                              <div style={{ fontSize: 11, color: C.text3, fontFamily: "monospace", marginTop: 2 }}>{src.type} · {src.path}</div>
                            </div>
                            <div style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 11, color: st.color, flexShrink: 0 }}>
                              <div style={{ width: 7, height: 7, borderRadius: "50%", backgroundColor: st.color }} />
                              {st.label}
                            </div>
                            <div style={{ fontSize: 11, color: C.text3, display: "flex", alignItems: "center", gap: 4, flexShrink: 0 }}>
                              <Clock size={10} />{formatSynced(src.syncedAt)}
                            </div>
                            <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                              <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }}>Edit</Btn>
                              <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px" }}>Test</Btn>
                              <Btn variant="subtle" style={{ fontSize: 11, padding: "3px 8px", color: C.danger }}>Remove</Btn>
                            </div>
                          </div>
                        );
                      })
                    )}
                    {mod.sources.length > 0 && (
                      <div style={{ padding: "8px 16px 8px 60px", borderTop: `1px solid ${C.border1}`, backgroundColor: C.bg }}>
                        <Btn variant="subtle" style={{ fontSize: 12 }}><Plus size={11} />Add another source</Btn>
                      </div>
                    )}
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
