using KDP_CATEGORY_RANKERData.Models;
using Microsoft.EntityFrameworkCore;

namespace KDP_CATEGORY_RANKERData.Repositories;

public class KeywordRepository : Repository<Keyword>, IKeywordRepository
{
    public KeywordRepository(KdpContext context) : base(context)
    {
    }

    public async Task<List<Keyword>> GetByMarketAsync(string market)
    {
        return await _dbSet
            .Where(k => k.Market == market)
            .Include(k => k.KeywordMetrics)
            .OrderBy(k => k.Text)
            .ToListAsync();
    }

    public async Task<Keyword?> GetByTextAndMarketAsync(string text, string market)
    {
        return await _dbSet
            .Include(k => k.KeywordMetrics)
            .FirstOrDefaultAsync(k => k.Text == text && k.Market == market);
    }

    public async Task<List<KeywordMetric>> GetKeywordHistoryAsync(int keywordId, int months = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);
        
        return await _context.KeywordMetrics
            .Where(km => km.KeywordId == keywordId && km.SnapshotAt >= cutoffDate)
            .OrderBy(km => km.Year)
            .ThenBy(km => km.Month)
            .ToListAsync();
    }
}

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(KdpContext context) : base(context)
    {
    }

    public async Task<List<Category>> GetByMarketAsync(string market)
    {
        return await _dbSet
            .Where(c => c.Market == market)
            .Include(c => c.CategorySnapshots)
            .OrderBy(c => c.Breadcrumb)
            .ToListAsync();
    }

    public async Task<Category?> GetByCanonicalIdAndMarketAsync(string canonicalId, string market)
    {
        return await _dbSet
            .Include(c => c.CategorySnapshots)
            .FirstOrDefaultAsync(c => c.CanonicalId == canonicalId && c.Market == market);
    }

    public async Task<List<Category>> GetGhostCategoriesAsync(string market)
    {
        return await _dbSet
            .Where(c => c.Market == market && c.IsGhost)
            .Include(c => c.CategorySnapshots)
            .OrderBy(c => c.Breadcrumb)
            .ToListAsync();
    }

    public async Task<List<Category>> GetDuplicateCategoriesAsync(string market)
    {
        return await _dbSet
            .Where(c => c.Market == market && c.IsDuplicateGroupId != null)
            .Include(c => c.CategorySnapshots)
            .OrderBy(c => c.IsDuplicateGroupId)
            .ThenBy(c => c.Breadcrumb)
            .ToListAsync();
    }

    public async Task<List<CategorySnapshot>> GetCategoryHistoryAsync(int categoryId, int months = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);
        
        return await _context.CategorySnapshots
            .Where(cs => cs.CategoryId == categoryId && cs.SnapshotAt >= cutoffDate)
            .OrderBy(cs => cs.Year)
            .ThenBy(cs => cs.Month)
            .ToListAsync();
    }
}

public class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(KdpContext context) : base(context)
    {
    }

    public async Task<List<Book>> GetByMarketAsync(string market)
    {
        return await _dbSet
            .Where(b => b.Market == market)
            .Include(b => b.BookSnapshots)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<Book?> GetByAsinAndMarketAsync(string asin, string market)
    {
        return await _dbSet
            .Include(b => b.BookSnapshots)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .Include(b => b.ReverseAsinTerms)
            .FirstOrDefaultAsync(b => b.ASIN == asin && b.Market == market);
    }

    public async Task<List<Book>> GetByCategoryAsync(int categoryId, string market)
    {
        return await _dbSet
            .Where(b => b.Market == market && b.BookCategories.Any(bc => bc.CategoryId == categoryId))
            .Include(b => b.BookSnapshots.OrderByDescending(bs => bs.CapturedAt).Take(1))
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<List<BookSnapshot>> GetBookHistoryAsync(string asin, string market, int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.BookSnapshots
            .Where(bs => bs.ASIN == asin && bs.Market == market && bs.CapturedAt >= cutoffDate)
            .OrderBy(bs => bs.CapturedAt)
            .ToListAsync();
    }
}