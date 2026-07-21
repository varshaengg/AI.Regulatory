// A6 — Permission Matrix. Rows = personas, columns = features, cells = verbs.
// Admins toggle checkboxes; each toggle PUT /permissions/matrix and updates UI.
import * as React from "react";
import { Save, ShieldCheck } from "lucide-react";
import { C } from "../design/tokens";
import { Card, ScreenCaption } from "../design/primitives";
import {
  listPersonas, listFeatures, listPermissionVerbs,
  getPermissionMatrix, togglePermission,
} from "../api/resources";
import type {
  Persona, Feature, Permission, PermissionMatrixEntry, PermissionCode,
} from "../api/types";
import { useApi, ErrorBanner } from "../api/useApi";
import { usePermissions } from "../api/usePermissions";

export default function A6Screen() {
  const perms = usePermissions();
  const canWrite = perms.hasPermission("UserManagement", "Admin");

  const personas    = useApi((sig) => listPersonas(sig), []);
  const features    = useApi((sig) => listFeatures(sig), []);
  const verbs       = useApi((sig) => listPermissionVerbs(sig), []);
  const matrixLoad  = useApi((sig) => getPermissionMatrix(sig), []);

  // Local mutable copy of matrix — updated optimistically on toggle
  const [matrix, setMatrix] = React.useState<PermissionMatrixEntry[] | null>(null);
  const [busy, setBusy]     = React.useState<string | null>(null);
  const [err, setErr]       = React.useState<string | null>(null);

  React.useEffect(() => {
    if (matrixLoad.status === "ready") setMatrix(matrixLoad.data);
  }, [matrixLoad]);

  const isGranted = (personaCode: string, featureCode: string, verb: PermissionCode) => {
    if (!matrix) return false;
    // Admin verb implies lower verbs at read time.
    if (matrix.some((m) => m.personaCode === personaCode && m.featureCode === featureCode
                        && m.permissionCode === "Admin" && m.granted)) return true;
    return matrix.some((m) => m.personaCode === personaCode && m.featureCode === featureCode
                           && m.permissionCode === verb && m.granted);
  };

  // Is the cell disabled because a higher verb (Admin) is also on?
  const isImpliedByAdmin = (personaCode: string, featureCode: string, verb: PermissionCode) => {
    if (verb === "Admin") return false;
    if (!matrix) return false;
    return matrix.some((m) => m.personaCode === personaCode && m.featureCode === featureCode
                           && m.permissionCode === "Admin" && m.granted);
  };

  const onToggle = async (personaCode: string, featureCode: string, verb: PermissionCode) => {
    if (!canWrite) return;
    const cellKey = `${personaCode}|${featureCode}|${verb}`;
    const current = isGranted(personaCode, featureCode, verb) && !isImpliedByAdmin(personaCode, featureCode, verb);
    const next = !current;
    setBusy(cellKey); setErr(null);
    // Optimistic update.
    setMatrix((prev) => {
      if (!prev) return prev;
      const others = prev.filter((m) => !(m.personaCode === personaCode
                                        && m.featureCode === featureCode
                                        && m.permissionCode === verb));
      return next
        ? [...others, { personaCode, featureCode, permissionCode: verb, granted: true }]
        : others;
    });
    try {
      await togglePermission({ personaCode, featureCode, permissionCode: verb, granted: next });
    } catch (e: any) {
      setErr(e?.message ?? String(e));
      // Rollback.
      setMatrix((prev) => {
        if (!prev) return prev;
        const others = prev.filter((m) => !(m.personaCode === personaCode
                                          && m.featureCode === featureCode
                                          && m.permissionCode === verb));
        return current
          ? [...others, { personaCode, featureCode, permissionCode: verb, granted: true }]
          : others;
      });
    } finally { setBusy(null); }
  };

  const loading = personas.status === "loading" || features.status === "loading"
               || verbs.status === "loading"    || matrixLoad.status === "loading";
  const errorMsg = personas.status === "error" ? personas.error
                : features.status === "error"  ? features.error
                : verbs.status === "error"     ? verbs.error
                : matrixLoad.status === "error" ? matrixLoad.error : null;

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="A6" persona="Admin" />
      <div>
        <h1 style={{ fontSize: 22, fontWeight: 600, color: C.text1, margin: 0 }}>Permission matrix</h1>
        <p style={{ fontSize: 13, color: C.text3, margin: "4px 0 0" }}>
          Configure feature access per persona. Changes apply immediately and drive left-nav visibility for every user.
          Admin implies all lower verbs on a cell.
        </p>
      </div>

      {!canWrite && (
        <div style={{
          marginTop: 16, padding: 10, borderRadius: 4,
          backgroundColor: C.warnTint, color: "#8A6100", fontSize: 12,
          display: "flex", alignItems: "center", gap: 8,
        }}>
          <ShieldCheck size={14} /> Read-only — you need <b>Admin</b> on User Management to edit this matrix.
        </div>
      )}
      {err && <ErrorBanner message={err} style={{ marginTop: 16 }} />}
      {errorMsg && <ErrorBanner message={errorMsg} style={{ marginTop: 16 }} />}

      {loading || !matrix || personas.status !== "ready" || features.status !== "ready" || verbs.status !== "ready" ? (
        <div style={{ marginTop: 20, padding: 16, fontSize: 13, color: C.text3, fontStyle: "italic" }}>Loading matrix…</div>
      ) : (
        <Card style={{ padding: 0, marginTop: 20, overflow: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 12 }}>
            <thead>
              <tr style={{ backgroundColor: C.bg2 }}>
                <th style={thHeader}>Persona</th>
                {features.data.map((f) => (
                  <th key={f.code} colSpan={verbs.data.length} style={{ ...thHeader, borderLeft: `1px solid ${C.border1}` }}>
                    <div style={{ fontWeight: 600, color: C.text1 }}>{f.name}</div>
                    <div style={{ fontWeight: 400, color: C.text3, fontSize: 10 }}>{f.category}</div>
                  </th>
                ))}
              </tr>
              <tr>
                <th style={thSubHeader}></th>
                {features.data.map((f) => (
                  <React.Fragment key={f.code}>
                    {verbs.data.map((v) => (
                      <th key={`${f.code}-${v.code}`} style={{ ...thSubHeader, borderLeft: `1px solid ${C.border1}`, minWidth: 56 }}>
                        {v.name}
                      </th>
                    ))}
                  </React.Fragment>
                ))}
              </tr>
            </thead>
            <tbody>
              {personas.data.map((p) => (
                <tr key={p.code} style={{ borderTop: `1px solid ${C.border1}` }}>
                  <td style={{ padding: "10px 12px", fontWeight: 500, color: C.text1, whiteSpace: "nowrap" }}>
                    <div>{p.name}</div>
                    <div style={{ fontSize: 10, color: C.text3, fontWeight: 400 }}>{p.description}</div>
                  </td>
                  {features.data.map((f) => (
                    <React.Fragment key={f.code}>
                      {verbs.data.map((v) => {
                        const verb = v.code as PermissionCode;
                        const implied = isImpliedByAdmin(p.code, f.code, verb);
                        const on = isGranted(p.code, f.code, verb);
                        const busyKey = `${p.code}|${f.code}|${verb}`;
                        return (
                          <td key={busyKey} style={{ ...tdCell, borderLeft: `1px solid ${C.border1}` }}>
                            <label style={{ display: "inline-flex", cursor: canWrite && !implied ? "pointer" : "not-allowed" }}>
                              <input
                                type="checkbox"
                                checked={on}
                                disabled={!canWrite || implied || busy === busyKey}
                                onChange={() => onToggle(p.code, f.code, verb)}
                                style={{ accentColor: C.brand, cursor: "inherit" }}
                                title={implied ? "Implied by Admin verb on this cell" : undefined}
                              />
                            </label>
                          </td>
                        );
                      })}
                    </React.Fragment>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      <div style={{ marginTop: 12, fontSize: 11, color: C.text3, display: "flex", alignItems: "center", gap: 6 }}>
        <Save size={12} /> Changes save automatically.
      </div>
    </div>
  );
}

const thHeader: React.CSSProperties = {
  padding: "10px 12px", textAlign: "left", fontSize: 11, fontWeight: 600, color: C.text2,
  textTransform: "uppercase", letterSpacing: "0.04em",
};

const thSubHeader: React.CSSProperties = {
  padding: "6px 8px", textAlign: "center", fontSize: 10, fontWeight: 500, color: C.text3,
  borderBottom: `1px solid ${C.border1}`, backgroundColor: C.bg2,
};

const tdCell: React.CSSProperties = {
  padding: "10px 8px", textAlign: "center", verticalAlign: "middle",
};
