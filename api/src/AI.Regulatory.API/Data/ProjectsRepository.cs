using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Projects — L1 tile + L2 catalog.</summary>
public sealed class ProjectsRepository : BaseRepository<ProjectDetail>
{
    private static readonly DateTime Now = DateTime.UtcNow;

    public ProjectsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(ProjectDetail item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ProjectDetail> SeedData() => new[]
    {
        Make("px-102-de", "PX-102 · DE · Initial",  "DE", "In progress", "PX-102", new[] {"M1","M2","M3","M4","M5"}, "Marcus L.", 55, -42, -1),
        Make("px-102-fr", "PX-102 · FR · Initial",  "FR", "Reviewing",   "PX-102", new[] {"M1","M2","M3"},           "Marcus L.", 82, -30, -6),
        Make("el-201-it", "EL-201 · IT · Renewal",  "IT", "Blocked",     "EL-201", new[] {"M1","M2","M3","M5"},      "Aisha K.",  35, -28, -4),
        Make("el-201-es", "EL-201 · ES · Variation","ES", "Draft",       "EL-201", new[] {"M2","M3"},                "Marcus L.", 18,  -8, -2),
        Make("px-102-nl", "PX-102 · NL · Variation","NL", "Submitted",   "PX-102", new[] {"M1","M3","M5"},           "Tom K.",   100, -70,-40),
        Make("cv-304-de", "CV-304 · DE · Initial",  "DE", "In progress", "CV-304", new[] {"M1","M2","M3","M4","M5"}, "Aisha K.",  72,  -6, -1),
        Make("cv-304-fr", "CV-304 · FR · Initial",  "FR", "Reviewing",   "CV-304", new[] {"M1","M2","M3"},           "Tom K.",    40,  -5, -1),
        Make("px-102-uk", "PX-102 · UK · Initial",  "UK", "Draft",       "PX-102", new[] {"M1","M2","M3","M4","M5"}, "Marcus L.",  0,  -2, -1),
    };

    private static ProjectDetail Make(string id, string name, string country, string status, string product,
        string[] modules, string owner, int pct, int createdDaysAgo, int updatedDaysAgo)
        => new(id, name, country, status, product, modules,
               $"{owner.Replace(" ", "").ToLower()}@ucatalyst.onmicrosoft.com",
               owner, pct,
               Now.AddDays(createdDaysAgo), Now.AddDays(updatedDaysAgo),
               "\"" + Guid.NewGuid().ToString("N")[..8] + "\"");
}
