namespace ReleaseNotes.Core.Catalog;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPrevious => Page > 1 && TotalItems > 0;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        T[] all = source as T[] ?? source.ToArray();
        T[] items = all.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return new PagedResult<T>(items, page, pageSize, all.Length);
    }

    public PagedResult<TResult> Map<TResult>(Func<T, TResult> map) =>
        new(Items.Select(map).ToArray(), Page, PageSize, TotalItems);
}
