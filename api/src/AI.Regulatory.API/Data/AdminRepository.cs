using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Admin dashboard data — A1.</summary>
public sealed class AdminRepository : BaseRepository<AdminActivity>
{
    public AdminRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(AdminActivity item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<AdminActivity> SeedData() => new[]
    {
        new AdminActivity("a1", "DE v4.2",   "Template uploaded by Sara M.",             DateTime.UtcNow.AddMinutes(-2)),
        new AdminActivity("a2", "M3 Source", "Connection test passed · PX-102",          DateTime.UtcNow.AddMinutes(-14)),
        new AdminActivity("a3", "FR v3.1",   "Archived · replaced by FR v4.0",           DateTime.UtcNow.AddHours(-1)),
        new AdminActivity("a4", "AI Search", "Reindex triggered by scheduler",           DateTime.UtcNow.AddHours(-3)),
    };

    /// <summary>Aggregated stat cards on A1. Deliberately derived at read-time from mocked domain state.</summary>
    public AdminStats GetStats() => new(
        TemplateCount: 42,
        CountriesMapped: 18,
        StorageAccounts: 6,
        PendingSetupTasks: 2);
}
