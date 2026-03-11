namespace eShopModernized.Models;

/// <summary>
/// Paginated items view model — migrated from eShopLegacyMVC.ViewModel.
/// Added generic constraint and record-like properties.
/// </summary>
public class PaginatedItemsViewModel<T> where T : class
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public long TotalItems { get; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;
    public IReadOnlyList<T> Data { get; }

    public PaginatedItemsViewModel(int pageIndex, int pageSize, long totalItems, IEnumerable<T> data)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalItems = totalItems;
        Data = data.ToList().AsReadOnly();
    }
}
