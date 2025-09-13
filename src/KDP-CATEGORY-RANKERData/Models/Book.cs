using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KDP_CATEGORY_RANKERData.Models;

[Table("Books")]
public class Book
{
    [Key]
    [MaxLength(20)]
    public string ASIN { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public int PagesOrMinutes { get; set; }

    public bool KUParticipation { get; set; }

    [MaxLength(50)]
    public string PublisherType { get; set; } = "Indie";

    [Column(TypeName = "decimal(3,2)")]
    public decimal RatingAvg { get; set; }

    public int RatingsCount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<BookSnapshot> BookSnapshots { get; set; } = new List<BookSnapshot>();
    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    public virtual ICollection<ReverseAsinTerm> ReverseAsinTerms { get; set; } = new List<ReverseAsinTerm>();
}

[Table("BookSnapshots")]
public class BookSnapshot
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [ForeignKey(nameof(Book))]
    public string ASIN { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public int BSR { get; set; }

    public int EstDailySales { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal EstMonthlyEarnings { get; set; }

    [MaxLength(1000)]
    public string CategoryIdsCsv { get; set; } = string.Empty;

    // Navigation properties
    public virtual Book Book { get; set; } = null!;
}

[Table("BookCategories")]
public class BookCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [ForeignKey(nameof(Book))]
    public string ASIN { get; set; } = string.Empty;

    [Required]
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Book Book { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
}

[Table("ReverseAsinTerms")]
public class ReverseAsinTerm
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [ForeignKey(nameof(Book))]
    public string ASIN { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Term { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;

    [Range(0, 100)]
    public int StrengthScore { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Book Book { get; set; } = null!;
}