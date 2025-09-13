using System.Linq.Expressions;

namespace KDP_CATEGORY_RANKERData.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync<TKey>(TKey id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    
    Task<T> AddAsync(T entity);
    Task<List<T>> AddRangeAsync(IEnumerable<T> entities);
    
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    
    Task<int> SaveChangesAsync();
}

public interface IKeywordRepository : IRepository<KDP_CATEGORY_RANKERData.Models.Keyword>
{
    Task<List<KDP_CATEGORY_RANKERData.Models.Keyword>> GetByMarketAsync(string market);
    Task<KDP_CATEGORY_RANKERData.Models.Keyword?> GetByTextAndMarketAsync(string text, string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.KeywordMetric>> GetKeywordHistoryAsync(int keywordId, int months = 12);
}

public interface ICategoryRepository : IRepository<KDP_CATEGORY_RANKERData.Models.Category>
{
    Task<List<KDP_CATEGORY_RANKERData.Models.Category>> GetByMarketAsync(string market);
    Task<KDP_CATEGORY_RANKERData.Models.Category?> GetByCanonicalIdAndMarketAsync(string canonicalId, string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.Category>> GetGhostCategoriesAsync(string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.Category>> GetDuplicateCategoriesAsync(string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.CategorySnapshot>> GetCategoryHistoryAsync(int categoryId, int months = 12);
}

public interface IBookRepository : IRepository<KDP_CATEGORY_RANKERData.Models.Book>
{
    Task<List<KDP_CATEGORY_RANKERData.Models.Book>> GetByMarketAsync(string market);
    Task<KDP_CATEGORY_RANKERData.Models.Book?> GetByAsinAndMarketAsync(string asin, string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.Book>> GetByCategoryAsync(int categoryId, string market);
    Task<List<KDP_CATEGORY_RANKERData.Models.BookSnapshot>> GetBookHistoryAsync(string asin, string market, int days = 30);
}