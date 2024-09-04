using System.Globalization;
using System.Text.Json;

namespace CfPerfCalc.Services;

public class RatingCache(JSInterop jsInterop)
{
    private Dictionary<string, int>? _cachedUsers; //lost on refresh

    public async Task<Dictionary<string, int>?> GetRatedListAsync()
    {
        if (_cachedUsers != null)
        {
            return _cachedUsers;
        }

        // Check if data is cached in localStorage
        var cachedData = await jsInterop.InvokeAsync<string>("localStorage.getItem", "cfRatedList");
        var cachedTimestamp = await jsInterop.InvokeAsync<string>("localStorage.getItem", "cfRatedListTimestamp");

        if (!string.IsNullOrEmpty(cachedData) && !string.IsNullOrEmpty(cachedTimestamp))
        {
            var cacheDate = DateTime.Parse(cachedTimestamp);
            if (cacheDate > DateTime.Now.AddDays(-7))
            {
                _cachedUsers = JsonSerializer.Deserialize<Dictionary<string, int>>(cachedData)!;
                return _cachedUsers;
            }
        }

        return null;
    }

    public async Task Update(Dictionary<string, int> dict)
    {
        _cachedUsers = dict;
        var serializedData = JsonSerializer.Serialize(_cachedUsers);
        await jsInterop.InvokeVoidAsync("localStorage.setItem", "cfRatedList", serializedData);
        await jsInterop.InvokeVoidAsync("localStorage.setItem", "cfRatedListTimestamp", DateTime.Now.ToString(CultureInfo.InvariantCulture));
    }
}