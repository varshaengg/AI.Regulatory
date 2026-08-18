// Auto-split from src/app/App.tsx - screen L3.
import * as React from "react";
import { useNavigate } from "react-router";
import { Archive, ArrowRight, FileText } from "lucide-react";
import { C } from "../design/tokens";
import { Btn, Chip, Card, Stepper, ScreenCaption, ProgressBar } from "../design/primitives";
import { archiveProject, createProject, getProject, updateProject } from "../api/resources";
import { usePermissions } from "../api/usePermissions";
import { ErrorBanner, useApi } from "../api/useApi";
import type { ProjectDetail } from "../api/types";

type FormState = {
  name: string;
  country: string;
  product: string;
  productVersion: string;
  submissionType: "Initial" | "Variation" | "Renewal";
  targetDate: string;
  owner: string;
};

type CountryOption = { code: string; label: string };

const COUNTRY_OPTIONS: CountryOption[] = [
  { code: "DE", label: "🇩🇪 Germany (DE)" },
  { code: "FR", label: "🇫🇷 France (FR)" },
  { code: "IT", label: "🇮🇹 Italy (IT)" },
  { code: "ES", label: "🇪🇸 Spain (ES)" },
  { code: "NL", label: "🇳🇱 Netherlands (NL)" },
  { code: "GB", label: "🇬🇧 United Kingdom (GB)" },
];

const TEMPLATE_ROWS = [
  { id: "M1", label: "Administrative", version: "4.2", color: C.brand },
  { id: "M2", label: "Summaries", version: "4.1", color: "#5C2E91" },
  { id: "M3", label: "Quality", version: "4.2", color: C.success },
  { id: "M4", label: "Nonclinical", version: "3.9", color: C.warn },
  { id: "M5", label: "Clinical", version: "4.0", color: C.danger },
];

function countryLabel(code: string): string {
  return COUNTRY_OPTIONS.find((item) => item.code === code)?.label ?? code;
}

function initialForm(project?: ProjectDetail | null): FormState {
  return project
    ? {
        name: project.name,
        country: project.country,
        product: project.product,
        productVersion: project.productVersion,
        submissionType: project.procedure,
        targetDate: project.targetSubmissionDate ?? "",
        owner: project.ownerDisplayName,
      }
    : {
        name: "",
        country: "",
        product: "",
        productVersion: "",
        submissionType: "Initial",
        targetDate: "",
        owner: "",
      };
}

export default function L3Screen() {
  const navigate = useNavigate();
  const perms = usePermissions();
  const canAdmin = perms.hasPermission("DossierManagement", "Admin");
  const canWrite = canAdmin || perms.hasPermission("DossierManagement", "Write");
  const [activeProjectId, setActiveProjectId] = React.useState<string | null>(() =>
    new URLSearchParams(window.location.search).get("projectId"));

  const project = useApi<ProjectDetail | null>(
    (sig) => (activeProjectId ? getProject(activeProjectId, sig) : Promise.resolve(null)),
    [activeProjectId],
  );

  const [form, setForm] = React.useState<FormState>(() => initialForm(null));
  const [submitting, setSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [persistedProject, setPersistedProject] = React.useState<ProjectDetail | null>(null);
  const [success, setSuccess] = React.useState<string | null>(null);
  const [hydratedProjectId, setHydratedProjectId] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (project.status === "ready" && project.data && hydratedProjectId !== activeProjectId) {
      setForm(initialForm(project.data));
      setPersistedProject(project.data);
      setHydratedProjectId(activeProjectId);
    }
    if (!activeProjectId) {
      setHydratedProjectId(null);
      setPersistedProject(null);
    }
  }, [activeProjectId, hydratedProjectId, project]);

  // While an existing project is being loaded, keep the form hidden behind a
  // progress bar instead of briefly showing blank/default field values.
  const isHydrating = Boolean(activeProjectId) && project.status !== "error" && hydratedProjectId !== activeProjectId;

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((current) => ({ ...current, [key]: value }));

  const submit = async (): Promise<ProjectDetail | null> => {
    if (!canWrite || submitting) return null;
    if (!form.name.trim() || !form.country.trim()) {
      setError("Project name and target country are required.");
      return null;
    }

    setError(null);
    setSuccess(null);
    setSubmitting(true);
    try {
      const request = {
        name: form.name.trim(),
        country: form.country,
        product: form.product.trim() || undefined,
        productVersion: form.productVersion.trim() || undefined,
        procedure: form.submissionType,
        targetSubmissionDate: form.targetDate || undefined,
        ownerDisplayName: form.owner.trim() || undefined,
      };
      const detail = persistedProject
        ? await updateProject(persistedProject.id, request, persistedProject.etag)
        : await createProject(request);
      setPersistedProject(detail);
      setActiveProjectId(detail.id);
      setHydratedProjectId(detail.id);
      setForm(initialForm(detail));
      window.history.replaceState({}, "", `${window.location.pathname}?projectId=${encodeURIComponent(detail.id)}`);
      setSuccess(persistedProject ? "Project changes saved." : `Created ${detail.name} as ${detail.id}.`);
      return detail;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save project.");
      return null;
    } finally {
      setSubmitting(false);
    }
  };

  const next = async () => {
    if (!canWrite && persistedProject) {
      navigate(`/screen/L4?projectId=${encodeURIComponent(persistedProject.id)}`);
      return;
    }

    const detail = await submit();
    if (detail) {
      navigate(`/screen/L4?projectId=${encodeURIComponent(detail.id)}`);
    }
  };

  const cancel = () => {
    setError(null);
    setSuccess(null);
    if (persistedProject) {
      setForm(initialForm(persistedProject));
      return;
    }
    window.history.back();
  };

  const archive = async () => {
    if (!persistedProject || !canAdmin || submitting) return;
    if (!window.confirm(`Archive ${persistedProject.name}?`)) return;

    setError(null);
    setSuccess(null);
    setSubmitting(true);
    try {
      await archiveProject(persistedProject.id);
      setActiveProjectId(null);
      setPersistedProject(null);
      setForm(initialForm(null));
      window.history.replaceState({}, "", window.location.pathname);
      setSuccess("Project archived.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to archive project.");
    } finally {
      setSubmitting(false);
    }
  };

  const fieldStyle: React.CSSProperties = {
    padding: "6px 10px",
    borderRadius: 4,
    border: `1px solid ${C.border1}`,
    fontSize: 13,
    color: C.text1,
    backgroundColor: "white",
    fontFamily: "inherit",
  };

  return (
    <div style={{ padding: 24 }}>
      <ScreenCaption id="L3" persona="RALead" />
      <div style={{ marginBottom: 24 }}><Stepper steps={["Basics", "Modules", "Review & Launch"]} active={0} /></div>

      {error && <ErrorBanner message={error} style={{ marginBottom: 16 }} />}
      {success && (
        <div style={{ marginBottom: 16, padding: 12, borderRadius: 4, backgroundColor: C.successTint, color: C.success, fontSize: 12 }}>
          {success}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "1fr 320px", gap: 24 }}>
        <Card style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
            <h3 style={{ fontSize: 15, fontWeight: 600, color: C.text1 }}>Project details</h3>
            {project.status === "ready" && project.data && (
              <Chip color="brand">Loaded from API · {project.data.id}</Chip>
            )}
          </div>

          {isHydrating ? (
            <ProgressBar label="Loading project details…" />
          ) : (
          <>
          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Project name</label>
            <input
              style={fieldStyle}
              value={form.name}
              onChange={(e) => setField("name", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-name"
            />
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Product</label>
            <input
              style={fieldStyle}
              value={form.product}
              onChange={(e) => setField("product", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-product"
            />
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Product version</label>
            <input
              style={fieldStyle}
              value={form.productVersion}
              onChange={(e) => setField("productVersion", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-product-version"
            />
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Target country</label>
            <select
              style={fieldStyle}
              value={form.country}
              onChange={(e) => setField("country", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-country"
            >
              <option value="">Select country…</option>
              {COUNTRY_OPTIONS.map((opt) => (
                <option key={opt.code} value={opt.code}>{opt.label}</option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2, display: "block", marginBottom: 8 }}>Submission type</label>
            <div style={{ display: "flex", gap: 24, flexWrap: "wrap" }}>
              {(["Initial", "Variation", "Renewal"] as const).map((opt) => (
                <label
                  key={opt}
                  style={{
                    display: "flex", alignItems: "center", gap: 8, fontSize: 13,
                    cursor: canWrite && !submitting ? "pointer" : "not-allowed",
                    color: C.text1, opacity: canWrite && !submitting ? 1 : 0.65,
                  }}
                >
                  <input
                    type="radio"
                    name="submissionType"
                    checked={form.submissionType === opt}
                    onChange={() => setField("submissionType", opt)}
                    disabled={!canWrite || submitting}
                    data-id={`project-procedure-${opt.toLowerCase()}`}
                  />
                  {opt}
                </label>
              ))}
            </div>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Target submission date</label>
            <input
              style={fieldStyle}
              type="date"
              value={form.targetDate}
              onChange={(e) => setField("targetDate", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-target-date"
            />
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            <label style={{ fontSize: 12, fontWeight: 500, color: C.text2 }}>Owner</label>
            <input
              style={fieldStyle}
              value={form.owner}
              onChange={(e) => setField("owner", e.target.value)}
              disabled={!canWrite || submitting}
              data-id="project-owner"
            />
          </div>
          </>
          )}
        </Card>

        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1, marginBottom: 12 }}>Templates assigned</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
              {TEMPLATE_ROWS.map((m, i, arr) => (
                <div key={m.id} style={{ display: "flex", alignItems: "center", gap: 10, padding: "7px 0", borderBottom: i < arr.length - 1 ? `1px solid ${C.border1}` : "none" }}>
                  <div style={{ width: 24, height: 24, borderRadius: "50%", backgroundColor: m.color, color: "white", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10, fontWeight: 700, flexShrink: 0 }}>{m.id}</div>
                  <span style={{ flex: 1, fontSize: 12, color: C.text1 }}>{m.label}</span>
                  <span style={{ fontSize: 11, fontFamily: "monospace", color: C.text3 }}>v{m.version}</span>
                  <FileText size={12} color={C.text3} />
                </div>
              ))}
            </div>
          </Card>

          <Card style={{ padding: 16 }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: C.text1, marginBottom: 12 }}>
              {persistedProject ? "Update request" : "Create request"}
            </h3>
            <div style={{ fontSize: 12, color: C.text2, marginBottom: 12 }}>
              This will create the lifecycle root record and hand off to module setup.
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
              <Btn variant="primary" disabled={!canWrite || submitting || isHydrating} onClick={submit} data-id="save-project">
                <ArrowRight size={13} />
                {submitting ? "Saving…" : persistedProject ? "Save changes" : "Create project"}
              </Btn>
              <Chip color={canWrite ? "brand" : "disabled"}>{canWrite ? "Write enabled" : "Read only"}</Chip>
              {project.status === "ready" && project.data && (
                <Chip color="success">{countryLabel(project.data.country)}</Chip>
              )}
            </div>
          </Card>
        </div>
      </div>

      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginTop: 24 }}>
        <div style={{ display: "flex", gap: 8 }}>
          {persistedProject && (
            <Btn
              variant="subtle"
              disabled={submitting}
              onClick={() => navigate(`/screen/A4?projectId=${encodeURIComponent(persistedProject.id)}`)}
              data-id="manage-project-sources"
            >
              <FileText size={13} />
              Manage sources
            </Btn>
          )}
          {canAdmin && persistedProject && (
            <Btn variant="subtle" disabled={submitting} onClick={archive} data-id="archive-project">
              <Archive size={13} />
              Archive project
            </Btn>
          )}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Btn variant="subtle" disabled={submitting} onClick={cancel} data-id="cancel-project">Cancel</Btn>
          <Btn variant="secondary" disabled={!canWrite || submitting || isHydrating} onClick={submit} data-id="save-project-draft">
            Save draft
          </Btn>
          <Btn
            variant="primary"
            disabled={submitting || isHydrating || (!persistedProject && !canWrite)}
            onClick={next}
            data-id="next-project-modules"
          >
            Next
            <ArrowRight size={13} />
          </Btn>
        </div>
      </div>
    </div>
  );
}
