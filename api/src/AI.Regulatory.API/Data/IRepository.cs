namespace AI.Regulatory.API.Data;

/// <summary>
/// Generic repository contract. All concrete repositories inherit from
/// <see cref="BaseRepository{T}"/> which implements this interface once
/// with the <c>IsMocked</c> branch.
/// </summary>
public interface IRepository<T>
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<T?> GetAsync(string id, CancellationToken ct = default);
    Task<T> AddAsync(T item, CancellationToken ct = default);
}
