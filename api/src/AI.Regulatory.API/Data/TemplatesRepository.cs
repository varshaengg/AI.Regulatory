using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>CTD template catalog — A2.</summary>
public sealed class TemplatesRepository : BaseRepository<CtdTemplate>
{
    public TemplatesRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(CtdTemplate item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<CtdTemplate> SeedData() => new[]
    {
        new CtdTemplate("de-4.2",     "DE",           "Europe", new[]{"1","2","3","4","5"}, "4.2", "Sara M.", D("2025-11-10"), "Active"),
        new CtdTemplate("fr-4.0",     "FR",           "Europe", new[]{"1","2","3","4","5"}, "4.0", "Sara M.", D("2025-10-22"), "Active"),
        new CtdTemplate("it-3.8",     "IT",           "Europe", new[]{"1","2","3","5"},      "3.8", "Tom K.",  D("2025-09-15"), "Active"),
        new CtdTemplate("es-3.6",     "ES",           "Europe", new[]{"1","2","3","4","5"}, "3.6", "Sara M.", D("2025-08-01"), "Draft"),
        new CtdTemplate("nl-3.5",     "NL",           "Europe", new[]{"1","2","3"},          "3.5", "Tom K.",  D("2025-07-20"), "Active"),
        new CtdTemplate("eu-4.2",     "EU (region)",  "Europe", new[]{"1","2","3","4","5"}, "4.2", "Sara M.", D("2025-06-01"), "Active"),
        new CtdTemplate("global-3.0", "Global",       "Global", new[]{"1","2","3","4","5"}, "3.0", "Admin",   D("2024-12-01"), "Active"),
        new CtdTemplate("uk-2.9",     "UK",           "MHRA",   new[]{"1","2","3","4","5"}, "2.9", "Tom K.",  D("2024-11-15"), "Archived"),
    };

    private static DateTime D(string iso) => DateTime.Parse(iso).ToUniversalTime();
}
