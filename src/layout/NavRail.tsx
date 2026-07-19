// Auto-split from src/app/App.tsx — left navigation rail.
import * as React from "react";
import { Link } from "react-router";

import { C, navGroups } from "../design/tokens";
export function NavRail({ activeScreen }: { activeScreen: string }) {
  return (
    <div style={{
      width: 260, flexShrink: 0, display: "flex", flexDirection: "column",
      borderRight: `1px solid ${C.border1}`, backgroundColor: "white", overflowY: "auto",
    }}>
      <div style={{ padding: "12px 16px", borderBottom: `1px solid ${C.border1}` }}>
        <div style={{ fontSize: "13px", fontWeight: 600, color: C.text1 }}>ARA · Wireframes</div>
        <div style={{ fontSize: "11px", color: C.text3 }}>Fluent 2 · React SPA + BFF · v1.5</div>
      </div>
      {navGroups.map(group => (
        <div key={group.label} style={{ paddingTop: "12px", paddingBottom: "4px" }}>
          <div style={{
            padding: "0 16px 4px", fontSize: "11px", fontWeight: 700,
            textTransform: "uppercase", letterSpacing: "0.06em", color: C.text3,
          }}>{group.label}</div>
          {group.items.map(item => {
            const isActive = item.id === activeScreen;
            return (
              <Link
                key={item.id}
                to={`/screen/${item.id}`}
                style={{
                  width: "100%", display: "flex", alignItems: "center", gap: "8px",
                  padding: "6px 16px", textAlign: "left", fontSize: "13px", cursor: "pointer",
                  backgroundColor: isActive ? C.brandTint : "transparent",
                  color: isActive ? C.brandPressed : C.text2,
                  fontWeight: isActive ? 600 : 400, border: "none", fontFamily: "inherit",
                  lineHeight: 1.4, textDecoration: "none", boxSizing: "border-box",
                }}
              >
                <span style={{ fontSize: "11px", width: 24, flexShrink: 0, color: C.text3 }}>{item.id}</span>
                <span style={{ flex: 1 }}>{item.label}</span>
                {item.star === 1 && <span style={{ fontSize: "12px" }}>⭐</span>}
                {item.star === 2 && <span style={{ fontSize: "12px" }}>⭐⭐</span>}
              </Link>
            );
          })}
        </div>
      ))}
    </div>
  );
}
