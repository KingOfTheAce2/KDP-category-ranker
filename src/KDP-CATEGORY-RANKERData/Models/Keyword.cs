using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KDP_CATEGORY_RANKERData.Models;

[Table("Keywords")]
public class Keyword
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Text { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<KeywordMetric> KeywordMetrics { get; set; } = new List<KeywordMetric>();
}

[Table("KeywordMetrics")]
public class KeywordMetric
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Keyword))]
    public int KeywordId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Market { get; set; } = string.Empty;

    [Required]
    public int Month { get; set; }

    [Required]
    public int Year { get; set; }

    public int Searches { get; set; }

    [Range(0, 100)]
    public int CompetitiveScore { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AvgMonthlyEarnings { get; set; }

    [Required]
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Keyword Keyword { get; set; } = null!;
}