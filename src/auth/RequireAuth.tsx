// Protected-route wrapper — kicks off an interactive login-redirect if the
// user isn't signed in yet.
import { useEffect, type ReactNode } from "react";
import {
  useMsal,
  useIsAuthenticated,
  AuthenticatedTemplate,
  UnauthenticatedTemplate,
} from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { loginRequest } from "./msalConfig";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { instance, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  useEffect(() => {
    // Only fire once initial redirect handling is complete.
    if (!isAuthenticated && inProgress === InteractionStatus.None) {
      instance.loginRedirect(loginRequest).catch((err) => {
        // eslint-disable-next-line no-console
        console.error("[MSAL] loginRedirect failed", err);
      });
    }
  }, [isAuthenticated, inProgress, instance]);

  return (
    <>
      <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      <UnauthenticatedTemplate>
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
          Redirecting to Microsoft sign-in…
        </div>
      </UnauthenticatedTemplate>
    </>
  );
}
