using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KDP_CATEGORY_RANKERData.Models;

[Table("Categories")]
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CanonicalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Breadcrumb { get; set; } = string.Empty;

    public bool IsGhost { get; set; }

    [MaxLength(100)]
    public string? IsDuplicateGroupId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CategorySnapshot> CategorySnapshots { get; set; } = new List<CategorySnapshot>();
    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}

[Table("CategorySnapshots")]
public class CategorySnapshot
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    public int Year { get; set; }

    public int SalesToNo1 { get; set; }

    public int SalesToNo10 { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AvgPriceIndie { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AvgPriceBig { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal AvgRating { get; set; }

    public int AvgPageCount { get; set; }

    public int AvgAgeDays { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal KuPct { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal LargePublisherPct { get; set; }

    [Required]
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Category Category { get; set; } = null!;
}