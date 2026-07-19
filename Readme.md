# Regulatory AI Assistant — Web

React SPA for the AI-assisted regulatory dossier preparation solution (CTD Modules 1–5). Talks to a .NET API deployed alongside it in the customer's Azure subscription (BYOC model — see `docs/` for the SDD).

The initial wireframes were generated with Figma Make from `docs/wireframes/figma-make-prompt.md` and are being progressively hardened into production code.

## Running locally

```pwsh
npm install
npm run dev
```

Then open http://localhost:5173/.

## Stack

- React 18 + TypeScript + Vite 6
- Tailwind CSS 4 + shadcn/ui (Radix primitives)
- MSAL (Entra ID sign-in) — scaffolded
- react-router — screens are routable per persona journey

## Layout

```
src/
  design/     tokens.ts, primitives.tsx
  layout/     AppBar.tsx, NavRail.tsx
  screens/    A1–A4, L1–L6, U1–U2, R1–R2
  app/        App.tsx (router)
  main.tsx
```
