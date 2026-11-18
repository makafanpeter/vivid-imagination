using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Shared.Domain.Infrastructure;

 [Serializable]
    public class PagedList<T> : List<T>, IPagedList<T>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="totalCount">Total count</param>
        public PagedList(IList<T> source, int pageIndex, int pageSize, int? totalCount = null)
        {

            pageIndex = Math.Max(pageIndex, 0);
            //min allowed page size is 1
            pageSize = Math.Max(pageSize, 1);

            TotalCount = totalCount ?? source.Count;
            TotalPages = TotalCount / pageSize;

            if (TotalCount % pageSize > 0)
                TotalPages++;

            PageSize = pageSize;
            PageIndex = pageIndex;
            AddRange(totalCount != null ? source : source.Skip(pageIndex * pageSize).Take(pageSize));
        }
        
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="totalCount">Total count</param>
        public PagedList(IQueryable<T> source, int pageIndex, int pageSize, int? totalCount = null)
        {

            pageIndex = Math.Max(pageIndex, 0);
            //min allowed page size is 1
            pageSize = Math.Max(pageSize, 1);

            TotalCount = totalCount ?? source.Count();
            TotalPages = TotalCount / pageSize;

            if (TotalCount % pageSize > 0)
                TotalPages++;

            PageSize = pageSize;
            PageIndex = pageIndex;
            AddRange(totalCount != null ? source : source.Skip(pageIndex * pageSize).Take(pageSize));
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        public static async Task<PagedList<T>> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var pagedList = new PagedList<T>(source, pageIndex, pageSize);
            return await Task.FromResult(pagedList);
        }

        


        /// <summary>
        /// Page index
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Total count
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Total pages
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Has previous page
        /// </summary>
        public bool HasPreviousPage => PageIndex > 0;

        /// <summary>
        /// Has next page
        /// </summary>
        public bool HasNextPage => PageIndex + 1 < TotalPages;


        public static IPagedList<T> Empty(int pageIndex, int pageSize)
        {
            pageIndex = Math.Max(pageIndex, 0);
            pageSize = Math.Max(pageSize, 1);
            return new PagedList<T>(new List<T>(), pageIndex, pageSize, 0);
        }
    }