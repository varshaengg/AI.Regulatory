// Effective permissions for the current user, cached in a React context so
// every consumer (NavRail, individual screens, action buttons) reads from the
// same fetch. One GET /api/v1/me/permissions on mount, then in-memory only.
//
// Persona → feature → verb resolution happens server-side (see
// PermissionMatrixRepository.GetEffectivePermissions in the .NET API). The
// SPA only holds the flat grants array and a lookup helper.

import * as React from "react";
import { getMyPermissions } from "./resources";
import type { MePermissions, PermissionCode } from "./types";

interface PermissionsContextValue {
  status: "loading" | "ready" | "error";
  me: MePermissions | null;
  hasPermission: (featureCode: string, verb: PermissionCode) => boolean;
  hasAny: (featureCode: string, verbs?: readonly PermissionCode[]) => boolean;
  refresh: () => void;
}

const noop: PermissionsContextValue = {
  status: "loading",
  me: null,
  hasPermission: () => false,
  hasAny: () => false,
  refresh: () => {},
};

const PermissionsContext = React.createContext<PermissionsContextValue>(noop);

export function PermissionsProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = React.useState<{ status: "loading" | "ready" | "error"; me: MePermissions | null }>(
    { status: "loading", me: null },
  );
  const [nonce, setNonce] = React.useState(0);

  React.useEffect(() => {
    const ac = new AbortController();
    setState({ status: "loading", me: null });
    getMyPermissions(ac.signal)
      .then((me) => { if (!ac.signal.aborted) setState({ status: "ready", me }); })
      .catch(() => { if (!ac.signal.aborted) setState({ status: "error", me: null }); });
    return () => ac.abort();
  }, [nonce]);

  const value = React.useMemo<PermissionsContextValue>(() => {
    const grantMap = new Map<string, Set<PermissionCode>>();
    for (const g of state.me?.grants ?? [])
      grantMap.set(g.featureCode.toLowerCase(), new Set(g.permissions));
    return {
      status: state.status,
      me: state.me,
      hasPermission: (feature, verb) =>
        grantMap.get(feature.toLowerCase())?.has(verb) ?? false,
      hasAny: (feature, verbs = ["Read", "Write", "Review", "Admin"]) => {
        const set = grantMap.get(feature.toLowerCase());
        if (!set) return false;
        return verbs.some((v) => set.has(v));
      },
      refresh: () => setNonce((n) => n + 1),
    };
  }, [state]);

  return <PermissionsContext.Provider value={value}>{children}</PermissionsContext.Provider>;
}

/** Access the current user's effective permissions. */
export function usePermissions(): PermissionsContextValue {
  return React.useContext(PermissionsContext);
}
