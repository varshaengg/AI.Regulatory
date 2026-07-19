// Top app bar — shows the real signed-in Entra user with a sign-out menu.
import * as React from "react";
import { useState, useRef, useEffect } from "react";
import { BookOpen, LogOut } from "lucide-react";
import { useMsal } from "@azure/msal-react";
import { C, screenConfig, type PersonaKey } from "../design/tokens";
import { Btn } from "../design/primitives";

function computeInitials(name?: string | null, username?: string | null): string {
  const source = (name ?? username ?? "").trim();
  if (!source) return "?";
  const parts = source.split(/[\s@._-]+/).filter(Boolean);
  if (parts.length === 0) return source[0]!.toUpperCase();
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return (parts[0]![0]! + parts[parts.length - 1]![0]!).toUpperCase();
}

export function AppBar({
  activeScreen,
  persona: _persona,
}: {
  activeScreen: string;
  persona: PersonaKey;
}) {
  const cfg = screenConfig[activeScreen];
  const { instance, accounts } = useMsal();
  const user = accounts[0];
  const displayName = user?.name ?? user?.username ?? "Signed-in user";
  const username = user?.username ?? "";
  const initials = computeInitials(user?.name, user?.username);

  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!menuOpen) return;
    const onClick = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, [menuOpen]);

  const signOut = () => {
    setMenuOpen(false);
    instance.logoutRedirect({ account: user }).catch((err) => {
      // eslint-disable-next-line no-console
      console.error("[MSAL] logoutRedirect failed", err);
    });
  };

  return (
    <div style={{
      height: 48, display: "flex", alignItems: "center", justifyContent: "space-between",
      padding: "0 16px", backgroundColor: "white", borderBottom: `1px solid ${C.border1}`,
      boxShadow: "0 2px 4px rgba(0,0,0,.06)", flexShrink: 0, zIndex: 10,
    }}>
      <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
        <div style={{
          width: 24, height: 24, borderRadius: "4px", backgroundColor: C.brand,
          display: "flex", alignItems: "center", justifyContent: "center",
          color: "white", fontSize: "13px", fontWeight: 700,
        }}>A</div>
        <span style={{ fontWeight: 600, fontSize: "13px", color: C.text1 }}>ARA</span>
        <span style={{ color: C.border2 }}>·</span>
        <span style={{ fontSize: "13px", color: C.text3 }}>{cfg.title}</span>
      </div>
      <div style={{ display: "flex", alignItems: "center", gap: "8px", position: "relative" }} ref={menuRef}>
        <Btn variant="secondary"><BookOpen size={13} />Docs</Btn>
        <button
          type="button"
          onClick={() => setMenuOpen((v) => !v)}
          title={`${displayName}${username ? ` (${username})` : ""}`}
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          style={{
            width: 28, height: 28, borderRadius: "50%", backgroundColor: C.brandTint,
            color: C.brandPressed, display: "flex", alignItems: "center", justifyContent: "center",
            fontSize: "11px", fontWeight: 600, border: "none", cursor: "pointer",
            fontFamily: "inherit",
          }}
        >
          {initials}
        </button>
        {menuOpen && (
          <div
            role="menu"
            style={{
              position: "absolute", top: 40, right: 0, minWidth: 240,
              backgroundColor: "white", border: `1px solid ${C.border1}`,
              borderRadius: 6, boxShadow: "0 8px 24px rgba(0,0,0,.16)",
              padding: 8, zIndex: 20,
            }}
          >
            <div style={{ padding: "8px 10px 10px" }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: C.text1, marginBottom: 2 }}>
                {displayName}
              </div>
              {username && (
                <div style={{ fontSize: 11, color: C.text3, wordBreak: "break-all" }}>
                  {username}
                </div>
              )}
            </div>
            <div style={{ height: 1, backgroundColor: C.border1, margin: "4px 0" }} />
            <button
              type="button"
              role="menuitem"
              onClick={signOut}
              style={{
                width: "100%", display: "flex", alignItems: "center", gap: 8,
                padding: "8px 10px", border: "none", background: "transparent",
                fontSize: 13, color: C.text1, cursor: "pointer", borderRadius: 4,
                fontFamily: "inherit", textAlign: "left",
              }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = C.bg2)}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "transparent")}
            >
              <LogOut size={13} color={C.text2} />
              Sign out
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
