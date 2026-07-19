using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Author assignments — U1.</summary>
public sealed class AssignmentsRepository : BaseRepository<Assignment>
{
    public AssignmentsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(Assignment item, string id)
        => string.Equals(item.SectionCode, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Assignment> SeedData() => new[]
    {
        new Assignment("3.2.S.4.2", "Analytical Procedures",                "PX-102 · M3 Quality",  "Low",  3,  true),
        new Assignment("3.2.S.4.3", "Validation of Analytical Procedures",  "PX-102 · M3 Quality",  "Med",  5,  false),
        new Assignment("3.2.P.2",   "Pharmaceutical Development",           "PX-102 · M3 Quality",  "High", 8,  false),
        new Assignment("5.3.1.1",   "Clinical Pharmacology Studies",        "PX-102 · M5 Clinical", "Med",  12, true),
        new Assignment("3.2.S.5",   "Reference Standards",                  "PX-102 · M3 Quality",  "Low",  2,  false),
    };
}
