using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.DTOs;

public record ReverseAsinResultDto
{
    public string Asin { get; init; } = string.Empty;
    public Market Market { get; init; }
    public List<TermResultDto> Terms { get; init; } = new();
    public List<string> RelatedAsins { get; init; } = new();
    public DateTime LastUpdated { get; init; } = DateTime.Now;
}

public record TermResultDto
{
    public string Term { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty; // "title", "subtitle", "series", "also-bought", "category", "review", "metadata"
    public int StrengthScore { get; init; } // 0-100
    public string Notes { get; init; } = string.Empty;
}

public record AmsKeywordResultDto
{
    public string Keyword { get; init; } = string.Empty;
    public string MatchType { get; init; } = "broad"; // broad, phrase, exact
    public string Source { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public int EstimatedSearches { get; init; }
    public decimal EstimatedCpc { get; init; }
}

public record AmsTargetingResultDto
{
    public string Asin { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty; // "competitor", "also-bought", "category"
    public decimal EstimatedCpc { get; init; }
    public string Notes { get; init; } = string.Empty;
}