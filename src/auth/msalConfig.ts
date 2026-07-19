// MSAL configuration — reads from Vite env vars.
// Copy .env.example to .env.local and fill in Client ID + tenant.
import { LogLevel, type Configuration } from "@azure/msal-browser";

const clientId = import.meta.env.VITE_MSAL_CLIENT_ID as string | undefined;
const tenantId = import.meta.env.VITE_MSAL_TENANT_ID as string | undefined;
const redirectUri =
  (import.meta.env.VITE_MSAL_REDIRECT_URI as string | undefined) ??
  window.location.origin;

if (!clientId || !tenantId) {
  // Surface a very loud error early — auth cannot work without these.
  // eslint-disable-next-line no-console
  console.error(
    "[MSAL] Missing VITE_MSAL_CLIENT_ID or VITE_MSAL_TENANT_ID. Copy .env.example to .env.local and fill them in."
  );
}

export const msalConfig: Configuration = {
  auth: {
    clientId: clientId ?? "00000000-0000-0000-0000-000000000000",
    authority: `https://login.microsoftonline.com/${tenantId ?? "common"}`,
    redirectUri,
    postLogoutRedirectUri: redirectUri,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: "sessionStorage", // keeps user signed in for the browser session
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        if (level === LogLevel.Error) {
          // eslint-disable-next-line no-console
          console.error("[MSAL]", message);
        }
      },
      logLevel: LogLevel.Warning,
    },
  },
};

// Scopes requested at sign-in time. User.Read is a Microsoft Graph delegated
// permission granted by default to any Entra user; use it to fetch the profile.
export const loginRequest = {
  scopes: ["User.Read"],
};

// Scopes requested when calling AI.Regulatory.API. Read from Vite env so the
// value can be swapped per environment without a rebuild.
const apiScope = import.meta.env.VITE_API_SCOPE as string | undefined;
export const apiRequest = {
  scopes: apiScope ? [apiScope] : [],
};

export const apiBaseUrl =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5100/api/v1";
