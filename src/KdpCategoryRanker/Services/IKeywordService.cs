namespace KdpCategoryRanker.Services;

public interface IKeywordService
{
    Task<List<string>> GetKeywordSuggestionsAsync(string query);
}