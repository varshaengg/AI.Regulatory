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

    /// <summary>Global default: hit the persistent store instead of loading seed data.</summary>
    public bool IsMocked { get; init; } = false;

    // NOTE: Per-repository LiveRepositories override removed — use Data:IsMocked
    // to control mocked vs live behavior globally. During migration the code may
    // still accept legacy configuration keys, but runtime uses the single flag.
}
