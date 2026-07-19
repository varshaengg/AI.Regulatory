using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Dossier runs — L6.</summary>
public sealed class RunsRepository : BaseRepository<DossierRun>
{
    public RunsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(DossierRun item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<DossierRun> SeedData() => new[]
    {
        new DossierRun(
            Id: "4821",
            ProjectId: "px-102-de",
            Status: "running",
            StartedAt: DateTime.UtcNow.AddMinutes(-6),
            Modules: new[]
            {
                new RunModule("M1", "Administrative", 8,  8,  100, "Complete",                                                   "ok"),
                new RunModule("M2", "Summaries",     14, 14,  100, "Complete",                                                   "ok"),
                new RunModule("M3", "Quality",        8, 12,   67, "Drafting section 3.2.S.4.2 — Analytical Procedures…",        "running"),
                new RunModule("M4", "Nonclinical",    3,  6,   50, "Blocked · citation missing in section 4.2.3.4",              "blocked"),
                new RunModule("M5", "Clinical",       4, 22,   18, "Drafting 5.3.1.1 — Clinical Pharmacology Studies…",          "running"),
            },
            Log: new[]
            {
                new RunLogEntry("12:04:12", "INFO",  "Orchestrator started · dossier run #4821"),
                new RunLogEntry("12:04:18", "OK",    "M1 Administrative complete · 8/8 sections drafted"),
                new RunLogEntry("12:04:52", "INFO",  "M2 Summaries — Section Drafter agent assigned"),
                new RunLogEntry("12:05:11", "OK",    "M2 Summaries complete · 14/14 sections drafted"),
                new RunLogEntry("12:05:18", "INFO",  "M3 Quality — starting · 12 sub-modules queued"),
                new RunLogEntry("12:05:33", "WARN",  "M3 3.2.S.4.2 low confidence 0.62 — flagged for review"),
                new RunLogEntry("12:06:01", "INFO",  "Citation Verifier checking 3.2.S.4.1 references…"),
                new RunLogEntry("12:06:44", "OK",    "Citation Verifier 3.2.S.4.1 · 4/4 citations verified"),
                new RunLogEntry("12:07:02", "WARN",  "M4 Nonclinical — section 4.2.3.1 source missing, skipping"),
                new RunLogEntry("12:07:15", "ERROR", "M4 4.2.3.4 citation missing — blocked, needs author review"),
                new RunLogEntry("12:08:30", "INFO",  "M5 Clinical — Section Drafter starting 22 sections…"),
            },
            Agents: new[]
            {
                new RunAgent("Orchestrator",        "running"),
                new RunAgent("Section Drafter",     "running"),
                new RunAgent("Citation Verifier",   "idle"),
                new RunAgent("Compliance Checker",  "idle"),
                new RunAgent("Compiler",            "idle"),
            }),
    };
}
