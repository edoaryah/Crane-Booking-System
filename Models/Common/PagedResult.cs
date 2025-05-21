using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models.Common
{
  public class PagedResult<T>
  {
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < PageCount;
  }
}
