namespace Logistics.Api.Common.Pagination;

public sealed record PaginationResponse<T>(IReadOnlyList<T> Items, int Pages, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}