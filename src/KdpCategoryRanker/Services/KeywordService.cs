namespace KdpCategoryRanker.Services;

public class KeywordService : IKeywordService
{
    public async Task<List<string>> GetKeywordSuggestionsAsync(string query)
    {
        await Task.Delay(500);

        return new List<string>
        {
            $"{query} guide",
            $"{query} tips",
            $"{query} strategies",
            $"{query} for beginners",
            $"how to {query}",
            $"{query} handbook",
            $"{query} mastery",
            $"complete {query}",
            $"{query} secrets",
            $"advanced {query}"
        };
    }
}