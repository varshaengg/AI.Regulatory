using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Static catalog of CTD modules (M1–M5). Used by L4/A4 UI to render the module strip.</summary>
public static class CtdModuleCatalog
{
    public static readonly IReadOnlyList<CtdModule> All = new[]
    {
        new CtdModule("M1", "Administrative", "#0F6CBD",  "Configured"),
        new CtdModule("M2", "Summaries",      "#5C2E91",  "Configured"),
        new CtdModule("M3", "Quality",        "#107C10",  "Needs input"),
        new CtdModule("M4", "Nonclinical",    "#B58500",  "Needs input"),
        new CtdModule("M5", "Clinical",       "#D13438",  "Configured"),
    };
}

/// <summary>Sub-module coverage for a specific project/module — L4 grid.</summary>
public sealed class SubModulesRepository : BaseRepository<SubModule>
{
    public SubModulesRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(SubModule item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    // For the demo, we seed the M3 Quality sub-modules for PX-102 / DE.
    protected override IEnumerable<SubModule> SeedData() => new[]
    {
        new SubModule("3.2.S.1",   "General information",              true,  "Found",   null),
        new SubModule("3.2.S.2",   "Manufacture",                      true,  "Found",   null),
        new SubModule("3.2.S.3",   "Characterisation",                 true,  "Partial", "Missing spectral data"),
        new SubModule("3.2.S.4",   "Control of drug substance",        true,  "Found",   null),
        new SubModule("3.2.S.4.1", "Specification",                    true,  "Found",   null),
        new SubModule("3.2.S.4.2", "Analytical procedures",            true,  "Found",   null),
        new SubModule("3.2.S.4.3", "Validation of analytical procedures", true, "Partial", null),
        new SubModule("3.2.S.5",   "Reference standards",              true,  "Missing", "Upload required"),
        new SubModule("3.2.P.1",   "Description and composition",      true,  "Found",   null),
        new SubModule("3.2.P.2",   "Pharmaceutical development",       false, "Missing", null),
        new SubModule("3.2.P.3",   "Manufacture",                      true,  "Found",   null),
        new SubModule("3.2.P.4",   "Control of excipients",            true,  "Partial", null),
    };
}
