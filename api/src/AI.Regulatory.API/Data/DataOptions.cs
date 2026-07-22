namespace AI.Regulatory.API.Data;

/// <summary>
/// Root data-layer configuration. Bound from the "Data" section of appsettings.
/// </summary>
/// <remarks>
/// <see cref="IsMocked"/> is the global default. <see cref="LiveRepositories"/>
/// lets you flip individual repos to live SQL while everything else stays on
/// seed data — useful during the phased migration from mocked to real
/// persistence. The branch itself lives only in <c>BaseRepository&lt;T&gt;</c>.
/// </remarks>
public sealed class DataOptions
{
    public const string SectionName = "Data";

    /// <summary>Global default: load seed data instead of hitting the persistent store.</summary>
    public bool IsMocked { get; init; } = true;

    /// <summary>
    /// Per-repository override — names in this list use the live store even when
    /// <see cref="IsMocked"/> is true. Values are simple class names, e.g.
    /// <c>"PersonasRepository"</c>, <c>"AppUsersRepository"</c>.
    /// </summary>
    public string[] LiveRepositories { get; init; } = Array.Empty<string>();
}
