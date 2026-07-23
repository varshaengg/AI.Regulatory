using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Template-method base for every data-layer repository.
///
/// <para>
/// This class is the <b>only</b> place in the codebase that checks
/// <see cref="DataOptions.IsMocked"/>. Concrete repositories inherit and
/// implement <see cref="SeedData"/> (for mocked mode) plus optionally
/// <see cref="ListFromStoreAsync"/> etc. (for live mode).
/// </para>
/// </summary>
public abstract class BaseRepository<T> : IRepository<T>
{
    private readonly bool _isMocked;
    private readonly Lazy<List<T>> _seed;

    protected BaseRepository(IOptions<DataOptions> options)
    {
        var opts = options.Value;
        // Single-flag model: the global IsMocked controls mocked vs live behavior.
        // During migration, repositories that have not implemented live store
        // operations will gracefully fallback to seed data (see callers).
        _isMocked = opts.IsMocked;
        // Lazy so the subclass constructor completes before SeedData runs.
        _seed = new Lazy<List<T>>(() => new List<T>(SeedData()));
    }

    /// <summary>Whether this repository is running against seed data.</summary>
    protected bool IsMocked => _isMocked;

    /// <summary>
    /// Live seed list — mutable in mocked mode. Concrete repositories may
    /// mutate this directly for operations the base <see cref="AddAsync"/>
    /// contract doesn't cover (updates, upserts, deletes) — but only when
    /// <see cref="IsMocked"/> is true.
    /// </summary>
    protected List<T> SeedList => _seed.Value;

    /// <summary>Seed values used when <c>IsMocked = true</c>.</summary>
    protected abstract IEnumerable<T> SeedData();

    /// <summary>Predicate used by <see cref="GetAsync"/> in mocked mode.</summary>
    protected abstract bool MatchesId(T item, string id);

    // ─── Public API ────────────────────────────────────────────────────────

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
    {
        if (_isMocked)
            return _seed.Value.AsReadOnly();

        try
        {
            return await ListFromStoreAsync(ct).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            // Graceful fallback: repository has no live implementation yet —
            // return seed data to avoid breaking the API during phased rollout.
            return _seed.Value.AsReadOnly();
        }
    }

    public virtual async Task<T?> GetAsync(string id, CancellationToken ct = default)
    {
        if (_isMocked)
            return _seed.Value.FirstOrDefault(x => MatchesId(x, id));

        try
        {
            return await GetFromStoreAsync(id, ct).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            return _seed.Value.FirstOrDefault(x => MatchesId(x, id));
        }
    }

    public virtual async Task<T> AddAsync(T item, CancellationToken ct = default)
    {
        if (_isMocked)
        {
            _seed.Value.Add(item);
            return item;
        }

        try
        {
            return await AddToStoreAsync(item, ct).ConfigureAwait(false);
        }
        catch (NotImplementedException)
        {
            // Fallback to in-memory seed when live store missing.
            _seed.Value.Add(item);
            return item;
        }
    }

    // ─── Store hooks — override when live storage lands ───────────────────

    protected virtual Task<IReadOnlyList<T>> ListFromStoreAsync(CancellationToken ct)
        => throw new NotImplementedException(
            $"Live store not yet implemented for {GetType().Name}. Set Data:IsMocked=true in configuration.");

    protected virtual Task<T?> GetFromStoreAsync(string id, CancellationToken ct)
        => throw new NotImplementedException(
            $"Live store not yet implemented for {GetType().Name}. Set Data:IsMocked=true in configuration.");

    protected virtual Task<T> AddToStoreAsync(T item, CancellationToken ct)
        => throw new NotImplementedException(
            $"Live store not yet implemented for {GetType().Name}. Set Data:IsMocked=true in configuration.");
}
