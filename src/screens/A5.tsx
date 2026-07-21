// A5 — User Management. Admins add customer-AD users to ARA and assign personas.
// See SDD §4.1 for schema + gating rules.
import * as React from "react";
import { UserPlus, Search, X, Trash2, Users } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, ScreenCaption } from "../design/primitives";
import {
  listUsers, listPersonas, createUser, assignUserPersonas, deleteUser, searchAadPeople,
} from "../api/resources";
import type { AppUser, Persona, AadPerson } from "../api/types";
import { useApi, ErrorBanner } from "../api/useApi";
import { usePermissions } from "../api/usePermissions";

const PERSONA_COLORS: Record<string, "success" | "warning" | "brand" | "neutral"> = {
  Admin: "brand",
  RaLead: "brand",
  RaAuthor: "success",
  RaReviewer: "warning",
};

export default function A5Screen() {
  const perms = usePermissions();
  const canWrite = perms.hasPermission("UserManagement", "Write")
                || perms.hasPermission("UserManagement", "Admin");

  const [refresh, setRefresh] = React.useState(0);
  const users    = useApi((sig) => listUsers(sig), [refresh]);
  const personas = useApi((sig) => listPersonas(sig), []);

  const [filter, setFilter] = React.useState("");
  const [personaFilter, setPersonaFilter] = React.useState<string>("");
  const [addOpen, setAddOpen] = React.useState(false);
  const [editUser, setEditUser] = React.useState<AppUser | null>(null);
  const [busyId, setBusyId] = React.useState<string | null>(null);
  const [err, setErr] = React.useState<string | null>(null);

  const filteredUsers = React.useMemo(() => {
    if (users.status !== "ready") return [];
    const q = filter.trim().toLowerCase();
    return users.data.filter((u) => {
      if (personaFilter && !u.personaCodes.includes(personaFilter)) return false;
      if (!q) return true;
      return u.displayName.toLowerCase().includes(q)
          || u.email.toLowerCase().includes(q)
          || (u.jobTitle ?? "").toLowerCase().includes(q);
    });
  }, [users, filter, personaFilter]);

  const onDelete = async (u: AppUser) => {
    if (!confirm(`Remove ${u.displayName} from ARA?`)) return;
    setBusyId(u.id); setErr(null);
    try { await deleteUser(u.id); setRefresh((n) => n + 1); }
    catch (e: any) { setErr(e?.message ?? String(e)); }
    finally { setBusyId(null); }
  };

  const personaLookup: Record<string, Persona> = React.useMemo(() => {
    if (personas.status !== "ready") return {};
    return Object.fromEntries(personas.data.map((p) => [p.code, p]));
  }, [personas]);

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A5" persona="Admin" />
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 4 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, margin: 0 }}>User management</h1>
          <p style={{ fontSize: 13, color: C.text3, margin: "4px 0 0" }}>
            Enrol tenant users and manage their persona assignments. People-picker is scoped to the customer AD only.
          </p>
        </div>
        {canWrite && (
          <Btn onClick={() => setAddOpen(true)}>
            <UserPlus size={14} /> Add user
          </Btn>
        )}
      </div>

      {err && <ErrorBanner message={err} style={{ marginTop: 16 }} />}

      {/* Filter row */}
      <Card style={{ padding: 12, marginTop: 20, marginBottom: 12 }}>
        <div style={{ display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap" }}>
          <div style={{ position: "relative", flex: 1, minWidth: 200 }}>
            <Search size={14} style={{ position: "absolute", left: 10, top: "50%", transform: "translateY(-50%)", color: C.text3 }} />
            <input
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder="Search by name, email, or job title"
              style={{
                width: "100%", padding: "6px 10px 6px 30px", fontSize: 13,
                border: `1px solid ${C.border1}`, borderRadius: 4, fontFamily: "inherit",
              }}
            />
          </div>
          <select
            value={personaFilter}
            onChange={(e) => setPersonaFilter(e.target.value)}
            style={{ padding: "6px 10px", fontSize: 13, border: `1px solid ${C.border1}`, borderRadius: 4, fontFamily: "inherit" }}
          >
            <option value="">All personas</option>
            {personas.status === "ready" && personas.data.map((p) => (
              <option key={p.code} value={p.code}>{p.name}</option>
            ))}
          </select>
          <span style={{ fontSize: 12, color: C.text3 }}>
            <Users size={12} style={{ verticalAlign: "middle", marginRight: 4 }} />
            {filteredUsers.length} of {users.status === "ready" ? users.data.length : "…"}
          </span>
        </div>
      </Card>

      {/* Users table */}
      <Card style={{ padding: 0, overflow: "hidden" }}>
        <div style={{
          display: "grid",
          gridTemplateColumns: "1.5fr 1.5fr 1.2fr 2fr 1fr auto",
          gap: "0 12px", padding: "10px 16px", fontSize: 11, fontWeight: 600, color: C.text3,
          textTransform: "uppercase", letterSpacing: "0.04em",
          borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg2,
        }}>
          <span>User</span><span>Email</span><span>Job title</span><span>Personas</span><span>Added</span><span></span>
        </div>
        {users.status === "loading" && <div style={{ padding: 16, fontSize: 12, color: C.text3, fontStyle: "italic" }}>Loading…</div>}
        {users.status === "error" && <div style={{ padding: 16 }}><ErrorBanner message={users.error} /></div>}
        {users.status === "ready" && filteredUsers.length === 0 && (
          <div style={{ padding: 24, textAlign: "center", fontSize: 13, color: C.text3 }}>No users match this filter.</div>
        )}
        {users.status === "ready" && filteredUsers.map((u, i, arr) => (
          <div key={u.id} style={{
            display: "grid",
            gridTemplateColumns: "1.5fr 1.5fr 1.2fr 2fr 1fr auto",
            gap: "0 12px", padding: "12px 16px", alignItems: "center", fontSize: 13,
            borderBottom: i < arr.length - 1 ? `1px solid ${C.border1}` : "none",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <div style={{
                width: 32, height: 32, borderRadius: "50%",
                backgroundColor: C.brandTint, color: C.brandPressed,
                display: "flex", alignItems: "center", justifyContent: "center",
                fontSize: 12, fontWeight: 600, flexShrink: 0,
              }}>{initials(u.displayName)}</div>
              <span style={{ color: C.text1, fontWeight: 500 }}>{u.displayName}</span>
            </div>
            <span style={{ color: C.text2 }}>{u.email}</span>
            <span style={{ color: C.text3 }}>{u.jobTitle ?? "—"}</span>
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
              {u.personaCodes.map((code) => (
                <Chip key={code} color={PERSONA_COLORS[code] ?? "neutral"}>
                  {personaLookup[code]?.name ?? code}
                </Chip>
              ))}
            </div>
            <span style={{ color: C.text3, fontSize: 12 }}>{formatDate(u.addedAt)}</span>
            <div style={{ display: "flex", gap: 6, justifyContent: "flex-end" }}>
              {canWrite && (
                <>
                  <Btn variant="secondary" onClick={() => setEditUser(u)}>Edit</Btn>
                  <button
                    onClick={() => onDelete(u)}
                    disabled={busyId === u.id}
                    title="Remove user"
                    style={{
                      padding: 6, border: `1px solid ${C.border1}`, borderRadius: 4,
                      backgroundColor: "white", cursor: busyId === u.id ? "wait" : "pointer",
                      color: C.danger,
                    }}
                  ><Trash2 size={14} /></button>
                </>
              )}
            </div>
          </div>
        ))}
      </Card>

      {addOpen && (
        <AddUserDialog
          personas={personas.status === "ready" ? personas.data : []}
          onClose={() => setAddOpen(false)}
          onCreated={() => { setAddOpen(false); setRefresh((n) => n + 1); }}
        />
      )}
      {editUser && (
        <EditPersonasDialog
          user={editUser}
          personas={personas.status === "ready" ? personas.data : []}
          onClose={() => setEditUser(null)}
          onSaved={() => { setEditUser(null); setRefresh((n) => n + 1); }}
        />
      )}
    </div>
  );
}

// ─── Add user dialog ─────────────────────────────────────────────────────────
function AddUserDialog({ personas, onClose, onCreated }: {
  personas: Persona[];
  onClose: () => void;
  onCreated: () => void;
}) {
  const [picked, setPicked] = React.useState<AadPerson | null>(null);
  const [selected, setSelected] = React.useState<string[]>([]);
  const [saving, setSaving] = React.useState(false);
  const [err, setErr] = React.useState<string | null>(null);

  const toggle = (code: string) => setSelected((s) =>
    s.includes(code) ? s.filter((c) => c !== code) : [...s, code]);

  const submit = async () => {
    if (!picked || selected.length === 0) return;
    setSaving(true); setErr(null);
    try {
      await createUser({
        aadObjectId: picked.aadObjectId,
        displayName: picked.displayName,
        email: picked.email,
        jobTitle: picked.jobTitle,
        personaCodes: selected,
      });
      onCreated();
    } catch (e: any) { setErr(e?.message ?? String(e)); }
    finally { setSaving(false); }
  };

  return (
    <ModalShell title="Add user" onClose={onClose}>
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        <div>
          <label style={{ fontSize: 12, fontWeight: 600, color: C.text2, display: "block", marginBottom: 4 }}>
            Find user in customer directory
          </label>
          <PeoplePicker value={picked} onChange={setPicked} />
        </div>
        <div>
          <label style={{ fontSize: 12, fontWeight: 600, color: C.text2, display: "block", marginBottom: 6 }}>
            Assign personas
          </label>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            {personas.map((p) => (
              <label key={p.code} style={{
                display: "inline-flex", alignItems: "center", gap: 6,
                padding: "6px 10px", border: `1px solid ${selected.includes(p.code) ? C.brand : C.border1}`,
                backgroundColor: selected.includes(p.code) ? C.brandTint : "white",
                borderRadius: 4, cursor: "pointer", fontSize: 13,
              }}>
                <input type="checkbox" checked={selected.includes(p.code)} onChange={() => toggle(p.code)} />
                <span style={{ color: C.text1 }}>{p.name}</span>
              </label>
            ))}
          </div>
        </div>
        {err && <ErrorBanner message={err} />}
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 8 }}>
          <Btn variant="secondary" onClick={onClose}>Cancel</Btn>
          <Btn onClick={submit} style={picked && selected.length ? {} : { opacity: 0.5, pointerEvents: "none" }}>
            {saving ? "Adding…" : "Add user"}
          </Btn>
        </div>
      </div>
    </ModalShell>
  );
}

// ─── Edit personas dialog ────────────────────────────────────────────────────
function EditPersonasDialog({ user, personas, onClose, onSaved }: {
  user: AppUser; personas: Persona[]; onClose: () => void; onSaved: () => void;
}) {
  const [selected, setSelected] = React.useState<string[]>(user.personaCodes);
  const [saving, setSaving] = React.useState(false);
  const [err, setErr] = React.useState<string | null>(null);

  const toggle = (code: string) => setSelected((s) =>
    s.includes(code) ? s.filter((c) => c !== code) : [...s, code]);

  const submit = async () => {
    setSaving(true); setErr(null);
    try {
      await assignUserPersonas(user.id, { personaCodes: selected });
      onSaved();
    } catch (e: any) { setErr(e?.message ?? String(e)); }
    finally { setSaving(false); }
  };

  return (
    <ModalShell title={`Edit personas — ${user.displayName}`} onClose={onClose}>
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        <div style={{ fontSize: 12, color: C.text3 }}>
          {user.email} · {user.jobTitle ?? "—"}
        </div>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          {personas.map((p) => (
            <label key={p.code} style={{
              display: "inline-flex", alignItems: "center", gap: 6,
              padding: "6px 10px", border: `1px solid ${selected.includes(p.code) ? C.brand : C.border1}`,
              backgroundColor: selected.includes(p.code) ? C.brandTint : "white",
              borderRadius: 4, cursor: "pointer", fontSize: 13,
            }}>
              <input type="checkbox" checked={selected.includes(p.code)} onChange={() => toggle(p.code)} />
              <span style={{ color: C.text1 }}>{p.name}</span>
            </label>
          ))}
        </div>
        {err && <ErrorBanner message={err} />}
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
          <Btn variant="secondary" onClick={onClose}>Cancel</Btn>
          <Btn onClick={submit}>{saving ? "Saving…" : "Save"}</Btn>
        </div>
      </div>
    </ModalShell>
  );
}

// ─── People picker (debounced /aad/people search) ────────────────────────────
function PeoplePicker({ value, onChange }: {
  value: AadPerson | null;
  onChange: (p: AadPerson | null) => void;
}) {
  const [query, setQuery] = React.useState("");
  const [results, setResults] = React.useState<AadPerson[]>([]);
  const [loading, setLoading] = React.useState(false);
  const [open, setOpen] = React.useState(false);

  React.useEffect(() => {
    if (!query.trim() || value) { setResults([]); return; }
    const ac = new AbortController();
    setLoading(true);
    const t = setTimeout(() => {
      searchAadPeople(query, 10, ac.signal)
        .then((r) => { if (!ac.signal.aborted) { setResults(r); setOpen(true); } })
        .catch(() => {})
        .finally(() => { if (!ac.signal.aborted) setLoading(false); });
    }, 250);
    return () => { clearTimeout(t); ac.abort(); };
  }, [query, value]);

  if (value) {
    return (
      <div style={{
        display: "flex", alignItems: "center", justifyContent: "space-between",
        border: `1px solid ${C.border1}`, borderRadius: 4, padding: "8px 12px",
      }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{
            width: 32, height: 32, borderRadius: "50%", backgroundColor: C.brandTint,
            color: C.brandPressed, display: "flex", alignItems: "center", justifyContent: "center",
            fontSize: 12, fontWeight: 600,
          }}>{initials(value.displayName)}</div>
          <div>
            <div style={{ fontSize: 13, fontWeight: 500, color: C.text1 }}>{value.displayName}</div>
            <div style={{ fontSize: 11, color: C.text3 }}>{value.email}{value.jobTitle ? ` · ${value.jobTitle}` : ""}</div>
          </div>
        </div>
        <button onClick={() => { onChange(null); setQuery(""); }}
                style={{ padding: 4, border: "none", background: "transparent", cursor: "pointer", color: C.text3 }}>
          <X size={16} />
        </button>
      </div>
    );
  }

  return (
    <div style={{ position: "relative" }}>
      <input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)}
        placeholder="Search customer directory (name or email)…"
        style={{
          width: "100%", padding: "8px 12px", fontSize: 13,
          border: `1px solid ${C.border1}`, borderRadius: 4, fontFamily: "inherit",
        }}
      />
      {open && (results.length > 0 || loading) && (
        <div style={{
          position: "absolute", top: "100%", left: 0, right: 0, zIndex: 10,
          border: `1px solid ${C.border1}`, backgroundColor: "white",
          borderRadius: 4, marginTop: 2, maxHeight: 260, overflowY: "auto",
          boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
        }}>
          {loading && <div style={{ padding: 10, fontSize: 12, color: C.text3, fontStyle: "italic" }}>Searching…</div>}
          {!loading && results.map((r) => (
            <div key={r.aadObjectId}
                 onClick={() => { onChange(r); setOpen(false); }}
                 style={{
                   padding: "8px 12px", cursor: "pointer", display: "flex", alignItems: "center", gap: 10,
                   borderBottom: `1px solid ${C.border1}`,
                 }}
                 onMouseEnter={(e) => e.currentTarget.style.backgroundColor = C.bg2}
                 onMouseLeave={(e) => e.currentTarget.style.backgroundColor = "white"}>
              <div style={{
                width: 28, height: 28, borderRadius: "50%", backgroundColor: C.brandTint,
                color: C.brandPressed, display: "flex", alignItems: "center", justifyContent: "center",
                fontSize: 11, fontWeight: 600,
              }}>{initials(r.displayName)}</div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 13, color: C.text1 }}>{r.displayName}</div>
                <div style={{ fontSize: 11, color: C.text3, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                  {r.email}{r.jobTitle ? ` · ${r.jobTitle}` : ""}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Modal shell ─────────────────────────────────────────────────────────────
function ModalShell({ title, onClose, children }: {
  title: string; onClose: () => void; children: React.ReactNode;
}) {
  return (
    <div onClick={onClose} style={{
      position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.45)",
      zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", padding: 24,
    }}>
      <div onClick={(e) => e.stopPropagation()} style={{
        width: "min(520px, 100%)", backgroundColor: "white", borderRadius: 6,
        boxShadow: "0 12px 40px rgba(0,0,0,0.2)", overflow: "hidden",
      }}>
        <div style={{
          display: "flex", alignItems: "center", justifyContent: "space-between",
          padding: "12px 16px", borderBottom: `1px solid ${C.border1}`,
        }}>
          <span style={{ fontSize: 14, fontWeight: 600, color: C.text1 }}>{title}</span>
          <button onClick={onClose} style={{
            padding: 4, border: "none", background: "transparent", cursor: "pointer", color: C.text3,
          }}><X size={16} /></button>
        </div>
        <div style={{ padding: 16 }}>{children}</div>
      </div>
    </div>
  );
}

function initials(name: string): string {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((s) => s[0]?.toUpperCase() ?? "").join("");
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  const days = Math.round((Date.now() - d.getTime()) / (1000 * 60 * 60 * 24));
  if (days === 0) return "Today";
  if (days === 1) return "Yesterday";
  if (days < 30) return `${days}d ago`;
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
}
