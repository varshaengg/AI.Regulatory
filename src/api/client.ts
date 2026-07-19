// Minimal typed API client — acquires an Entra access token via MSAL and
// attaches it as Bearer on every request. Throws `ApiError` on non-2xx.

import type { IPublicClientApplication, AccountInfo } from "@azure/msal-browser";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { apiRequest, apiBaseUrl } from "../auth/msalConfig";
import { ApiError, type ProblemDetails } from "./types";

let _msal: IPublicClientApplication | null = null;

/** Called once from AuthProvider after MSAL initialises. */
export function setMsalInstance(instance: IPublicClientApplication) {
  _msal = instance;
}

function activeAccount(): AccountInfo {
  if (!_msal) throw new Error("MSAL not initialised — call setMsalInstance first.");
  const account = _msal.getActiveAccount() ?? _msal.getAllAccounts()[0];
  if (!account) throw new Error("No signed-in account.");
  return account;
}

async function bearerToken(): Promise<string> {
  const msal = _msal!;
  const account = activeAccount();
  try {
    const result = await msal.acquireTokenSilent({ ...apiRequest, account });
    return result.accessToken;
  } catch (err) {
    if (err instanceof InteractionRequiredAuthError) {
      await msal.acquireTokenRedirect({ ...apiRequest, account });
      throw new Error("Redirecting to re-authenticate…");
    }
    throw err;
  }
}

type Query = Record<string, string | number | boolean | undefined | null>;

function buildUrl(path: string, query?: Query): string {
  const url = new URL(`${apiBaseUrl}${path}`);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null) url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}

async function request<TResponse>(
  method: string,
  path: string,
  options: { query?: Query; body?: unknown; signal?: AbortSignal } = {},
): Promise<TResponse> {
  const token = await bearerToken();
  const headers: Record<string, string> = {
    Accept: "application/json",
    Authorization: `Bearer ${token}`,
  };
  if (options.body !== undefined) headers["Content-Type"] = "application/json";

  const res = await fetch(buildUrl(path, options.query), {
    method,
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    signal: options.signal,
  });

  if (res.status === 204) return undefined as TResponse;

  const text = await res.text();
  const parsed = text ? (JSON.parse(text) as unknown) : undefined;

  if (!res.ok) {
    const problem: ProblemDetails =
      parsed && typeof parsed === "object"
        ? (parsed as ProblemDetails)
        : { status: res.status, title: res.statusText };
    throw new ApiError(res.status, problem);
  }
  return parsed as TResponse;
}

export const api = {
  get:  <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>("GET", path, { query, signal }),
  post: <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>("POST", path, { body, signal }),
  put:  <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>("PUT", path, { body, signal }),
  patch:<T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>("PATCH", path, { body, signal }),
  del:  <T>(path: string, signal?: AbortSignal) => request<T>("DELETE", path, { signal }),
};
