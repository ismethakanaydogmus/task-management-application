namespace TaskManager.Application.Common;

// Sayfalı liste cevabı. Frontend sayfa kontrollerini (önceki/sonraki, "Sayfa 2 / 5") bu bilgilerle çizer.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
