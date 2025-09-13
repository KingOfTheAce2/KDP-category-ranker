using FluentAssertions;
using KDP_CATEGORY_RANKERData;
using KDP_CATEGORY_RANKERData.Models;
using KDP_CATEGORY_RANKERData.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KDP_CATEGORY_RANKERTests.Integration;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly KdpContext _context;
    private readonly IKeywordRepository _keywordRepository;

    public DatabaseIntegrationTests()
    {
        var builder = Host.CreateApplicationBuilder();
        
        builder.Services.AddDbContext<KdpContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        builder.Services.AddScoped<IKeywordRepository, KeywordRepository>();
        
        _host = builder.Build();
        _context = _host.Services.GetRequiredService<KdpContext>();
        _keywordRepository = _host.Services.GetRequiredService<IKeywordRepository>();
        
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task KeywordRepository_AddAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var keyword = new Keyword
        {
            Text = "test keyword",
            Market = "com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var added = await _keywordRepository.AddAsync(keyword);
        await _keywordRepository.SaveChangesAsync();
        
        var retrieved = await _keywordRepository.GetByTextAndMarketAsync("test keyword", "com");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Text.Should().Be("test keyword");
        retrieved.Market.Should().Be("com");
        retrieved.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task KeywordRepository_GetByMarket_ReturnsCorrectResults()
    {
        // Arrange
        var keywords = new[]
        {
            new Keyword { Text = "keyword1", Market = "com", CreatedAt = DateTime.UtcNow },
            new Keyword { Text = "keyword2", Market = "com", CreatedAt = DateTime.UtcNow },
            new Keyword { Text = "keyword3", Market = "co.uk", CreatedAt = DateTime.UtcNow }
        };

        foreach (var keyword in keywords)
        {
            await _keywordRepository.AddAsync(keyword);
        }
        await _keywordRepository.SaveChangesAsync();

        // Act
        var comKeywords = await _keywordRepository.GetByMarketAsync("com");
        var ukKeywords = await _keywordRepository.GetByMarketAsync("co.uk");

        // Assert
        comKeywords.Should().HaveCount(2);
        comKeywords.All(k => k.Market == "com").Should().BeTrue();
        
        ukKeywords.Should().HaveCount(1);
        ukKeywords.Single().Market.Should().Be("co.uk");
    }

    public void Dispose()
    {
        _context.Dispose();
        _host.Dispose();
    }
}