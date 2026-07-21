// Auto-split from src/app/App.tsx — design tokens, persona registry, screen catalog, nav groups.
export const C = {
  brand: "#0F6CBD",
  brandHover: "#115EA3",
  brandPressed: "#0C3B5E",
  brandTint: "#EBF3FA",
  white: "#FFFFFF",
  bg: "#FAFAFA",
  bg2: "#F5F5F5",
  bg3: "#F0F0F0",
  border1: "#E0E0E0",
  border2: "#D1D1D1",
  disabled: "#ABABAB",
  text1: "#242424",
  text2: "#424242",
  text3: "#616161",
  textDis: "#707070",
  success: "#107C10",
  successTint: "#DFF6DD",
  warn: "#F7630C",
  warnTint: "#FFF4CE",
  danger: "#C50F1F",
  dangerTint: "#FDE7E9",
};

export const personas = {
  Admin: { label: "Admin · Sara", bg: "#EEE0FF", text: "#5C2E91" },
  RALead: { label: "RA Lead · Marcus", bg: "#EBF3FA", text: "#0C3B5E" },
  RAAuthor: { label: "RA Author · Priya", bg: "#DFF6DD", text: "#0B5A0B" },
  RAReviewer: { label: "RA Reviewer · Dr. Chen", bg: "#FFF4CE", text: "#8A6100" },
} as const;

export type PersonaKey = keyof typeof personas;

export const screenConfig: Record<string, { title: string; persona: PersonaKey; intent: string }> = {
  A1: { title: "Admin Home", persona: "Admin", intent: "Entry point for tenant administrators to verify configuration health at a glance." },
  A2: { title: "CTD Template Catalog", persona: "Admin", intent: "Each module (M1–M5) has its own template — view, replace, archive, or upload per module." },
  A3: { title: "Upload Template", persona: "Admin", intent: "Upload a new template file for a specific CTD module." },
  A4: { title: "Project Sources", persona: "RALead", intent: "Map default source storage locations to each CTD module — project-specific settings owned by the RA Lead." },
  A5: { title: "User Management", persona: "Admin", intent: "Manage tenant users and their persona assignments. People-picker sources are scoped to the customer AD tenant only (SDD §4.1)." },
  A6: { title: "Permission Matrix", persona: "Admin", intent: "Configure per-persona feature permissions (Read / Write / Review / Admin). Drives left-nav visibility and in-screen action gating via /me/permissions." },
  L1: { title: "Home Dashboard", persona: "RALead", intent: "Command centre showing active run status, assignments, and notifications at a glance." },
  L2: { title: "Projects List", persona: "RALead", intent: "Filterable catalogue of all dossier projects with progress bars and status chips." },
  L3: { title: "New Dossier · Basics", persona: "RALead", intent: "Wizard step 1 captures product, country, submission type, and owner metadata." },
  L4: { title: "New Dossier · Modules", persona: "RALead", intent: "Hero screen — per-module template override, source mapping, and section coverage table." },
  L5: { title: "New Dossier · Launch", persona: "RALead", intent: "Final read-only review of all wizard inputs with cost estimate before launch." },
  L6: { title: "Run Detail · Live", persona: "RALead", intent: "Real-time monitoring of agent progress, sub-section counts, and terminal log." },
  U1: { title: "Review Assignments", persona: "RAAuthor", intent: "Three-pane workspace: assignments list, inline section editor, and AI copilot chat." },
  U2: { title: "Copilot Focused", persona: "RAAuthor", intent: "Expanded copilot with rich citation cards, confidence gauge, and slash-command bar." },
  R1: { title: "Compiled Package", persona: "RAReviewer", intent: "Document viewer with collapsible module tree, validation summary, and export actions." },
  R2: { title: "Sign-off", persona: "RAReviewer", intent: "Attestation and Entra ID authenticated signature to lock the dossier package." },
};

export const navGroups: {
  label: string;
  /**
   * Feature code that gates every item in this group. Users lacking
   * `Read` on this feature see the whole group hidden. `undefined` = always show.
   */
  featureCode?: string;
  items: { id: string; label: string; star?: 1 | 2; featureCode?: string }[];
}[] = [
  { label: "Admin", featureCode: undefined, items: [
    { id: "A1", label: "Admin home" },
    { id: "A2", label: "CTD templates", featureCode: "Templates" },
    { id: "A3", label: "Upload template", featureCode: "Templates" },
    { id: "A5", label: "User management", featureCode: "UserManagement" },
    { id: "A6", label: "Permission matrix", featureCode: "UserManagement" },
  ]},
  { label: "RA Lead", featureCode: "DossierManagement", items: [
    { id: "L1", label: "Home dashboard" },
    { id: "L2", label: "Projects list" },
    { id: "A4", label: "Project sources" },
    { id: "L3", label: "New dossier · Basics" },
    { id: "L4", label: "New dossier · Modules", star: 1 },
    { id: "L5", label: "New dossier · Launch" },
    { id: "L6", label: "Run detail · Live" },
  ]},
  { label: "RA Author", featureCode: "Assignments", items: [
    { id: "U1", label: "Review assignments", star: 2 },
    { id: "U2", label: "Copilot focused" },
  ]},
  { label: "RA Reviewer", featureCode: "Reviews", items: [
    { id: "R1", label: "Compiled package" },
    { id: "R2", label: "Sign-off" },
  ]},
];
