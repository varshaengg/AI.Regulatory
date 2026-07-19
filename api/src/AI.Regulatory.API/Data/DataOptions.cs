namespace AI.Regulatory.API.Data;

/// <summary>
/// Root data-layer configuration. Bound from the "Data" section of appsettings.
/// </summary>
/// <remarks>
/// <see cref="IsMocked"/> is the single toggle every repository respects.
/// When true, repos return in-memory seed data (for demos and integration tests).
/// When false, repos delegate to the real persistent store (EF Core / Azure SQL) —
/// each concrete repository implements its own store overrides. The branch itself
/// lives only in <c>BaseRepository&lt;T&gt;</c>.
/// </remarks>
public sealed class DataOptions
{
    public const string SectionName = "Data";

    /// <summary>Load seed data instead of hitting the persistent store.</summary>
    public bool IsMocked { get; init; } = true;
}
