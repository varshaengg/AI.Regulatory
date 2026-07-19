// Auto-split from src/app/App.tsx — shared UI primitives.
import * as React from "react";
import { ChevronDown, ChevronRight, Check } from "lucide-react";
import { C, personas, screenConfig, type PersonaKey } from "./tokens";

type BtnVariant = "primary" | "secondary" | "subtle" | "danger";
export function Btn({ variant = "primary" as BtnVariant, children, onClick, style = {} }: {
  variant?: BtnVariant; children: React.ReactNode; onClick?: () => void; style?: React.CSSProperties;
}) {
  const base: React.CSSProperties = {
    display: "inline-flex", alignItems: "center", gap: "6px",
    padding: "5px 12px", fontSize: "13px", fontWeight: 500,
    borderRadius: "4px", cursor: "pointer", border: "1px solid",
    lineHeight: 1.4, fontFamily: "inherit",
  };
  const variants: Record<BtnVariant, React.CSSProperties> = {
    primary: { backgroundColor: C.brand, color: "white", borderColor: "transparent" },
    secondary: { backgroundColor: "white", color: C.text1, borderColor: C.border1 },
    subtle: { backgroundColor: "transparent", color: C.brand, borderColor: "transparent" },
    danger: { backgroundColor: C.danger, color: "white", borderColor: "transparent" },
  };
  return (
    <button style={{ ...base, ...variants[variant], ...style }} onClick={onClick}>
      {children}
    </button>
  );
}

type ChipColor = "success" | "warning" | "danger" | "brand" | "neutral" | "disabled";
export function Chip({ color = "neutral" as ChipColor, children }: { color?: ChipColor; children: React.ReactNode }) {
  const styles: Record<ChipColor, { bg: string; text: string }> = {
    success: { bg: C.successTint, text: C.success },
    warning: { bg: C.warnTint, text: "#8A6100" },
    danger: { bg: C.dangerTint, text: C.danger },
    brand: { bg: C.brandTint, text: C.brandPressed },
    neutral: { bg: C.bg3, text: C.text2 },
    disabled: { bg: C.bg2, text: C.textDis },
  };
  const s = styles[color] ?? styles.neutral;
  return (
    <span style={{
      display: "inline-flex", alignItems: "center", padding: "1px 8px",
      borderRadius: "999px", fontSize: "11px", fontWeight: 500,
      backgroundColor: s.bg, color: s.text,
    }}>{children}</span>
  );
}

export function PersonaBadge({ persona }: { persona: PersonaKey }) {
  const p = personas[persona];
  return (
    <span style={{
      display: "inline-flex", alignItems: "center", padding: "3px 10px",
      borderRadius: "999px", fontSize: "11px", fontWeight: 600,
      backgroundColor: p.bg, color: p.text,
    }}>{p.label}</span>
  );
}

export function Card({ children, style = {}, className = "" }: {
  children: React.ReactNode; style?: React.CSSProperties; className?: string;
}) {
  return (
    <div className={className} style={{
      backgroundColor: "white", borderRadius: "6px",
      border: `1px solid ${C.border1}`, boxShadow: "0 2px 4px rgba(0,0,0,.10)",
      ...style,
    }}>{children}</div>
  );
}

export function FInput({ label, placeholder }: { label?: string; placeholder?: string }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "4px" }}>
      {label && <label style={{ fontSize: "12px", fontWeight: 500, color: C.text2 }}>{label}</label>}
      <input
        style={{
          padding: "5px 10px", borderRadius: "4px", border: `1px solid ${C.border1}`,
          fontSize: "13px", color: C.text1, backgroundColor: "white", outline: "none",
          fontFamily: "inherit",
        }}
        placeholder={placeholder}
        readOnly
      />
    </div>
  );
}

export function FSelect({ label, value }: { label?: string; value: string }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "4px" }}>
      {label && <label style={{ fontSize: "12px", fontWeight: 500, color: C.text2 }}>{label}</label>}
      <div style={{
        padding: "5px 10px", borderRadius: "4px", border: `1px solid ${C.border1}`,
        fontSize: "13px", color: C.text1, backgroundColor: "white",
        display: "flex", alignItems: "center", justifyContent: "space-between", cursor: "pointer",
      }}>
        <span>{value}</span>
        <ChevronDown size={13} color={C.text3} />
      </div>
    </div>
  );
}

export function Stepper({ steps, active }: { steps: string[]; active: number }) {
  return (
    <div style={{ display: "flex", alignItems: "center" }}>
      {steps.map((step, i) => {
        const isDone = i < active;
        const isActive = i === active;
        return (
          <div key={i} style={{ display: "flex", alignItems: "center" }}>
            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <div style={{
                width: 28, height: 28, borderRadius: "50%",
                display: "flex", alignItems: "center", justifyContent: "center",
                fontSize: "12px", fontWeight: 600,
                backgroundColor: isDone ? C.success : isActive ? C.brand : C.bg3,
                color: isDone || isActive ? "white" : C.text3,
              }}>
                {isDone ? <Check size={13} /> : i + 1}
              </div>
              <span style={{
                fontSize: "13px", color: isActive ? C.brand : isDone ? C.text2 : C.text3,
                fontWeight: isActive ? 600 : 400,
              }}>{step}</span>
            </div>
            {i < steps.length - 1 && (
              <div style={{ width: 64, height: 2, margin: "0 12px", backgroundColor: isDone ? C.success : C.border1 }} />
            )}
          </div>
        );
      })}
    </div>
  );
}

export function Breadcrumb({ items }: { items: string[] }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: "4px", fontSize: "12px", marginBottom: "12px" }}>
      {items.map((item, i) => (
        <span key={i} style={{ display: "flex", alignItems: "center", gap: "4px" }}>
          {i < items.length - 1
            ? <span style={{ color: C.brand, cursor: "pointer" }}>{item}</span>
            : <span style={{ color: C.text1, fontWeight: 600 }}>{item}</span>}
          {i < items.length - 1 && <ChevronRight size={12} color={C.text3} />}
        </span>
      ))}
    </div>
  );
}

export function ScreenCaption({ id, persona }: { id: string; persona: PersonaKey }) {
  const cfg = screenConfig[id];
  return (
    <div style={{ display: "flex", alignItems: "center", gap: "10px", marginBottom: "16px", flexWrap: "wrap" }}>
      <span style={{
        fontSize: "11px", fontWeight: 700, padding: "2px 8px", borderRadius: "4px",
        backgroundColor: C.bg3, color: C.text3, letterSpacing: "0.04em",
      }}>{id}</span>
      <PersonaBadge persona={persona} />
      <span style={{ fontSize: "12px", color: C.text3 }}>{cfg.intent}</span>
    </div>
  );
}
