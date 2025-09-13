using KDP_CATEGORY_RANKERData.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KDP_CATEGORY_RANKERData.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<KdpContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? 
                             "Data Source=kdp-data.db"));

        // Add repositories
        services.AddScoped<IKeywordRepository, KeywordRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }

    public static async Task<IServiceProvider> EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KdpContext>();
        
        await context.Database.EnsureCreatedAsync();
        
        return serviceProvider;
    }

    public static async Task<IServiceProvider> SeedDatabaseAsync(this IServiceProvider serviceProvider, bool force = false)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KdpContext>();
        
        if (force || !await context.Keywords.AnyAsync())
        {
            await SeedDataAsync(context);
        }
        
        return serviceProvider;
    }

    private static async Task SeedDataAsync(KdpContext context)
    {
        // Seed sample keywords
        var sampleKeywords = new[]
        {
            new { Text = "romance novel", Market = "com" },
            new { Text = "fantasy book", Market = "com" },
            new { Text = "self help", Market = "com" },
            new { Text = "cookbook", Market = "com" },
            new { Text = "mystery thriller", Market = "com" },
            new { Text = "children's book", Market = "com" },
            new { Text = "business guide", Market = "com" },
            new { Text = "science fiction", Market = "com" },
        };

        foreach (var keywordData in sampleKeywords)
        {
            var keyword = new Models.Keyword
            {
                Text = keywordData.Text,
                Market = keywordData.Market,
                CreatedAt = DateTime.UtcNow
            };

            context.Keywords.Add(keyword);
        }

        // Seed sample categories
        var sampleCategories = new[]
        {
            new { CanonicalId = "1", Breadcrumb = "Books > Romance", Market = "com", IsGhost = false },
            new { CanonicalId = "2", Breadcrumb = "Books > Science Fiction & Fantasy", Market = "com", IsGhost = false },
            new { CanonicalId = "3", Breadcrumb = "Books > Self-Help", Market = "com", IsGhost = false },
            new { CanonicalId = "4", Breadcrumb = "Books > Cookbooks, Food & Wine", Market = "com", IsGhost = false },
            new { CanonicalId = "5", Breadcrumb = "Books > Mystery, Thriller & Suspense", Market = "com", IsGhost = false },
            new { CanonicalId = "6", Breadcrumb = "Books > Children's Books", Market = "com", IsGhost = false },
            new { CanonicalId = "7", Breadcrumb = "Books > Business & Money", Market = "com", IsGhost = false },
            new { CanonicalId = "ghost1", Breadcrumb = "Hidden Category", Market = "com", IsGhost = true },
        };

        foreach (var categoryData in sampleCategories)
        {
            var category = new Models.Category
            {
                CanonicalId = categoryData.CanonicalId,
                Breadcrumb = categoryData.Breadcrumb,
                Market = categoryData.Market,
                IsGhost = categoryData.IsGhost,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(category);
        }

        // Seed sample books
        var sampleBooks = new[]
        {
            new { ASIN = "B0123456789", Market = "com", Title = "The Ultimate Romance Guide", Author = "Jane Smith", Price = 9.99m },
            new { ASIN = "B0123456790", Market = "com", Title = "Fantasy World Chronicles", Author = "John Doe", Price = 12.99m },
            new { ASIN = "B0123456791", Market = "com", Title = "Self-Help Mastery", Author = "Bob Wilson", Price = 14.99m },
            new { ASIN = "B0123456792", Market = "com", Title = "Delicious Recipes", Author = "Chef Maria", Price = 19.99m },
            new { ASIN = "B0123456793", Market = "com", Title = "Mystery of the Lost Key", Author = "Detective Brown", Price = 8.99m },
        };

        foreach (var bookData in sampleBooks)
        {
            var book = new Models.Book
            {
                ASIN = bookData.ASIN,
                Market = bookData.Market,
                Title = bookData.Title,
                Author = bookData.Author,
                Price = bookData.Price,
                PagesOrMinutes = Random.Shared.Next(150, 400),
                KUParticipation = Random.Shared.NextDouble() > 0.6,
                PublisherType = Random.Shared.NextDouble() > 0.8 ? "Big 5" : "Indie",
                RatingAvg = (decimal)(Random.Shared.NextDouble() * 2 + 3),
                RatingsCount = Random.Shared.Next(50, 2000),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Books.Add(book);
        }

        await context.SaveChangesAsync();
    }
}