// Shared React hook + presentational helpers for API-backed screens.
// Removes the loading/error boilerplate that would otherwise repeat in
// every screen after the L1/API wiring.

import * as React from "react";
import { ApiError } from "./types";
import { C } from "../design/tokens";

export type AsyncState<T> =
  | { status: "loading"; data: null; error: null }
  | { status: "ready";   data: T;    error: null }
  | { status: "error";   data: null; error: string };

/** Fetch on mount (and when deps change), aborting on unmount. */
export function useApi<T>(
  fetcher: (signal: AbortSignal) => Promise<T>,
  deps: React.DependencyList = [],
): AsyncState<T> {
  const [state, setState] = React.useState<AsyncState<T>>({ status: "loading", data: null, error: null });

  React.useEffect(() => {
    const ac = new AbortController();
    setState({ status: "loading", data: null, error: null });
    fetcher(ac.signal)
      .then((data) => {
        if (!ac.signal.aborted) setState({ status: "ready", data, error: null });
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const msg = err instanceof ApiError
          ? `${err.status} ${err.problem.title ?? err.message}`
          : err instanceof Error ? err.message : String(err);
        setState({ status: "error", data: null, error: msg });
      });
    return () => ac.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

/** Uniform inline banner used to render fetch errors inside cards. */
export function ErrorBanner({ message, style }: { message: string; style?: React.CSSProperties }) {
  return (
    <div style={{
      padding: 10, borderRadius: 4, backgroundColor: C.dangerTint, color: C.danger, fontSize: 12,
      ...style,
    }}>
      Failed to load: {message}
    </div>
  );
}

/** Inline "Loading…" label matching the RA table styling. */
export function LoadingLabel({ children, style }: { children?: React.ReactNode; style?: React.CSSProperties }) {
  return (
    <span style={{ color: C.text3, fontStyle: "italic", fontSize: 12, ...style }}>
      {children ?? "Loading…"}
    </span>
  );
}
