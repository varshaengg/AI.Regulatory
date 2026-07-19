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
        _isMocked = options.Value.IsMocked;
        // Lazy so the subclass constructor completes before SeedData runs.
        _seed = new Lazy<List<T>>(() => new List<T>(SeedData()));
    }

    /// <summary>Whether this repository is running against seed data.</summary>
    protected bool IsMocked => _isMocked;

    /// <summary>Seed values used when <c>IsMocked = true</c>.</summary>
    protected abstract IEnumerable<T> SeedData();

    /// <summary>Predicate used by <see cref="GetAsync"/> in mocked mode.</summary>
    protected abstract bool MatchesId(T item, string id);

    // ─── Public API ────────────────────────────────────────────────────────

    public virtual Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => _isMocked
            ? Task.FromResult<IReadOnlyList<T>>(_seed.Value.AsReadOnly())
            : ListFromStoreAsync(ct);

    public virtual Task<T?> GetAsync(string id, CancellationToken ct = default)
        => _isMocked
            ? Task.FromResult(_seed.Value.FirstOrDefault(x => MatchesId(x, id)))
            : GetFromStoreAsync(id, ct);

    public virtual Task<T> AddAsync(T item, CancellationToken ct = default)
    {
        if (_isMocked)
        {
            _seed.Value.Add(item);
            return Task.FromResult(item);
        }
        return AddToStoreAsync(item, ct);
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
