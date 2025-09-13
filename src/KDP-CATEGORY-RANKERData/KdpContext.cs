using KDP_CATEGORY_RANKERData.Models;
using Microsoft.EntityFrameworkCore;

namespace KDP_CATEGORY_RANKERData;

public class KdpContext : DbContext
{
    public KdpContext(DbContextOptions<KdpContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Keyword> Keywords { get; set; } = null!;
    public DbSet<KeywordMetric> KeywordMetrics { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<CategorySnapshot> CategorySnapshots { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<BookSnapshot> BookSnapshots { get; set; } = null!;
    public DbSet<BookCategory> BookCategories { get; set; } = null!;
    public DbSet<ReverseAsinTerm> ReverseAsinTerms { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Keyword entity configuration
        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Text, e.Market }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // KeywordMetric entity configuration
        modelBuilder.Entity<KeywordMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.KeywordId, e.Market, e.Month, e.Year }).IsUnique();
            entity.HasIndex(e => e.SnapshotAt);
            
            entity.HasOne(e => e.Keyword)
                  .WithMany(k => k.KeywordMetrics)
                  .HasForeignKey(e => e.KeywordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Category entity configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CanonicalId, e.Market }).IsUnique();
            entity.HasIndex(e => e.IsDuplicateGroupId);
            entity.HasIndex(e => e.IsGhost);
            entity.HasIndex(e => e.UpdatedAt);
        });

        // CategorySnapshot entity configuration
        modelBuilder.Entity<CategorySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CategoryId, e.Month, e.Year }).IsUnique();
            entity.HasIndex(e => e.SnapshotAt);
            
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.CategorySnapshots)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Book entity configuration
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => new { e.ASIN, e.Market });
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Author);
            entity.HasIndex(e => e.UpdatedAt);
        });

        // BookSnapshot entity configuration
        modelBuilder.Entity<BookSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ASIN, e.Market, e.CapturedAt });
            entity.HasIndex(e => e.BSR);
            entity.HasIndex(e => e.CapturedAt);
            
            entity.HasOne(e => e.Book)
                  .WithMany(b => b.BookSnapshots)
                  .HasForeignKey(e => new { e.ASIN, e.Market })
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BookCategory entity configuration
        modelBuilder.Entity<BookCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ASIN, e.CategoryId }).IsUnique();
            
            entity.HasOne(e => e.Book)
                  .WithMany(b => b.BookCategories)
                  .HasForeignKey(e => new { e.ASIN })
                  .HasPrincipalKey(b => new { b.ASIN })
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.BookCategories)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ReverseAsinTerm entity configuration
        modelBuilder.Entity<ReverseAsinTerm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ASIN, e.Market, e.Term }).IsUnique();
            entity.HasIndex(e => e.Term);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.StrengthScore);
            
            entity.HasOne(e => e.Book)
                  .WithMany(b => b.ReverseAsinTerms)
                  .HasForeignKey(e => new { e.ASIN, e.Market })
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Setting entity configuration
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.HasIndex(e => e.UpdatedAt);
        });
    }
}