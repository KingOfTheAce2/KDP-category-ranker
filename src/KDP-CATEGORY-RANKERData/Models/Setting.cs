using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KDP_CATEGORY_RANKERData.Models;

[Table("Settings")]
public class Setting
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string ValueJson { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}