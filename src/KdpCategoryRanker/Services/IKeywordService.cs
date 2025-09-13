using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KdpCategoryRanker.Services;

public interface IKeywordService
{
    Task<List<string>> GetKeywordSuggestionsAsync(string query);
}