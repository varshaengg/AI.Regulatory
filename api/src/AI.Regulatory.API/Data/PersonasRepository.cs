using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Static registry of application personas — A5/A6.</summary>
/// <remarks>
/// Seed mirrors the MERGE statements in
/// <c>data/sql/AI.Regulatory.Sql/Script.PostDeployment.sql</c>. Any change here
/// must be reflected there so mocked and live modes agree.
/// </remarks>
public sealed class PersonasRepository : BaseRepository<Persona>
{
    public PersonasRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(Persona item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Persona> SeedData() => new[]
    {
        new Persona("Admin",      "Administrator", "Full system administration",   true),
        new Persona("RaLead",     "RA Lead",       "Regulatory affairs lead",      true),
        new Persona("RaAuthor",   "RA Author",     "Regulatory affairs author",    true),
        new Persona("RaReviewer", "RA Reviewer",   "Regulatory affairs reviewer",  true),
    };
}
