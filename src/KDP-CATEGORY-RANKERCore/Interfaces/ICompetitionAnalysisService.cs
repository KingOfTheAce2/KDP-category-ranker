using KDP_CATEGORY_RANKERCore.DTOs;
using KDP_CATEGORY_RANKERCore.Enums;

namespace KDP_CATEGORY_RANKERCore.Interfaces;

public interface ICompetitionAnalysisService
{
    Task<List<BookResultDto>> AnalyzeCompetitionAsync(string keyword, Market market, int maxResults = 30);
    Task<List<BookResultDto>> AnalyzeCategoryCompetitionAsync(string categoryId, Market market, int maxResults = 30);
    Task<BookResultDto?> GetBookDetailsAsync(string asin, Market market);
    Task<List<BookResultDto>> GetTopBooksInCategoryAsync(string categoryId, Market market, int count = 100);
    Task ExportCompetitionDataAsync(List<BookResultDto> books, string filePath);
}

public interface IReverseAsinService
{
    Task<ReverseAsinResultDto> AnalyzeAsinAsync(string asin, Market market);
    Task<List<ReverseAsinResultDto>> AnalyzeAsinsAsync(List<string> asins, Market market);
    Task<List<TermResultDto>> ExtractTermsFromTitleAsync(string title, string subtitle = "");
    Task<List<TermResultDto>> ExtractTermsFromReviewsAsync(string asin, Market market);
    Task<List<string>> GetAlsoBoughtAsinsAsync(string asin, Market market);
}

public interface IAmsKeywordService
{
    Task<List<AmsKeywordResultDto>> GenerateKeywordsAsync(
        List<string> seedKeywords, 
        List<string> competitorAuthors, 
        List<string> categoryTerms,
        Market market);
    
    Task<List<AmsTargetingResultDto>> GenerateAsinTargetsAsync(List<string> competitorAsins, Market market);
    Task<List<AmsKeywordResultDto>> GenerateNegativeKeywordsAsync(List<string> excludeTerms);
    Task ExportAmsKeywordsAsync(List<AmsKeywordResultDto> keywords, string filePath);
    Task ExportAmsTargetsAsync(List<AmsTargetingResultDto> targets, string filePath);
}