using System;
using System.Collections.Generic;

namespace cgrmodellibrary.DTOs.Common;

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }=5;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
