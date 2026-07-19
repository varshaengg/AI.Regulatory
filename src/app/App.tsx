import { Suspense, lazy } from "react";
import {
  createBrowserRouter,
  RouterProvider,
  Navigate,
  Outlet,
  useParams,
} from "react-router";
import { AppBar } from "../layout/AppBar";
import { NavRail } from "../layout/NavRail";
import { C, screenConfig } from "../design/tokens";
import { RequireAuth } from "../auth/RequireAuth";

// ─── Lazy-loaded screens ──────────────────────────────────────────────────────
const screenLoaders: Record<string, React.LazyExoticComponent<React.ComponentType>> = {
  A1: lazy(() => import("../screens/A1")),
  A2: lazy(() => import("../screens/A2")),
  A3: lazy(() => import("../screens/A3")),
  A4: lazy(() => import("../screens/A4")),
  L1: lazy(() => import("../screens/L1")),
  L2: lazy(() => import("../screens/L2")),
  L3: lazy(() => import("../screens/L3")),
  L4: lazy(() => import("../screens/L4")),
  L5: lazy(() => import("../screens/L5")),
  L6: lazy(() => import("../screens/L6")),
  U1: lazy(() => import("../screens/U1")),
  U2: lazy(() => import("../screens/U2")),
  R1: lazy(() => import("../screens/R1")),
  R2: lazy(() => import("../screens/R2")),
};

// ─── Layout shell (header + nav rail + screen outlet) ─────────────────────────
function Shell() {
  const params = useParams<{ id: string }>();
  const activeScreen = params.id ?? "A1";
  const cfg = screenConfig[activeScreen] ?? screenConfig.A1;

  return (
    <div
      style={{
        height: "100vh",
        display: "flex",
        flexDirection: "column",
        fontFamily: "'Inter', 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif",
        fontSize: 14,
        lineHeight: 1.4,
        backgroundColor: C.bg,
      }}
    >
      <AppBar activeScreen={activeScreen} persona={cfg.persona} />
      <div style={{ display: "flex", flex: 1, minHeight: 0 }}>
        <NavRail activeScreen={activeScreen} />
        <main style={{ flex: 1, overflowY: "auto" }}>
          <Suspense
            fallback={
              <div style={{ padding: 24, color: C.text3, fontSize: 13 }}>
                Loading screen…
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  );
}

function ScreenRoute() {
  const params = useParams<{ id: string }>();
  const id = params.id ?? "A1";
  const Screen = screenLoaders[id];
  if (!Screen) return <Navigate to="/screen/A1" replace />;
  return <Screen />;
}

// ─── Router config ────────────────────────────────────────────────────────────
const router = createBrowserRouter([
  {
    path: "/",
    element: (
      <RequireAuth>
        <Shell />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <Navigate to="/screen/A1" replace /> },
      { path: "screen/:id", element: <ScreenRoute /> },
      { path: "*", element: <Navigate to="/screen/A1" replace /> },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
