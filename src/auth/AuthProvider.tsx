// Wraps the app in MsalProvider after initializing the MSAL instance and
// processing any pending redirect response. Also exports a helper for
// screens that want to require the user to be signed in.
import { useEffect, useState, type ReactNode } from "react";
import {
  PublicClientApplication,
  EventType,
  type AuthenticationResult,
} from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { msalConfig } from "./msalConfig";
import { setMsalInstance } from "../api/client";

const msalInstance = new PublicClientApplication(msalConfig);
// Hand the same instance to the API client so it can acquire tokens.
setMsalInstance(msalInstance);

// Ensure the initial ready-state Promise runs before we render anything that
// might call MSAL APIs.
const initPromise = (async () => {
  await msalInstance.initialize();
  // Handle the redirect response from Microsoft (returning from login).
  await msalInstance.handleRedirectPromise();
  // If we don't have an active account but do have any cached one, activate it.
  if (!msalInstance.getActiveAccount()) {
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) msalInstance.setActiveAccount(accounts[0]);
  }
})();

// Whenever a login succeeds, make that account the active one so hooks pick it up.
msalInstance.addEventCallback((event) => {
  if (
    event.eventType === EventType.LOGIN_SUCCESS &&
    event.payload &&
    "account" in event.payload
  ) {
    const payload = event.payload as AuthenticationResult;
    msalInstance.setActiveAccount(payload.account);
  }
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  useEffect(() => {
    initPromise.then(() => setReady(true));
  }, []);

  if (!ready) {
    return (
      <div
        style={{
          height: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontFamily:
            "'Inter', 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif",
          color: "#616161",
          fontSize: 13,
        }}
      >
        Initializing authentication…
      </div>
    );
  }

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
