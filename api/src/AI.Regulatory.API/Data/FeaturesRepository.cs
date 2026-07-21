using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Feature catalog — the vertical axis of the permission matrix.</summary>
public sealed class FeaturesRepository : BaseRepository<Feature>
{
    public FeaturesRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(Feature item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Feature> SeedData() => new[]
    {
        new Feature("UserManagement",    "User Management",    "Administration", 10),
        new Feature("DossierManagement", "Dossier Management", "Regulatory",     20),
        new Feature("Templates",         "Templates",          "Regulatory",     30),
        new Feature("Assignments",       "Assignments",        "Regulatory",     40),
        new Feature("Reviews",           "Reviews",            "Regulatory",     50),
        new Feature("Notifications",     "Notifications",      "Platform",       60),
    };
}
