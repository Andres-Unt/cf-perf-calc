using System.Text.Json;
using Microsoft.JSInterop;

namespace CfPerfCalc.Services
{
    public class PerfCache
    {
        private readonly IJSRuntime _jsRuntime;
        private Dictionary<int, Dictionary<string, int>> _cache = new();
        private bool _initialized;

        public PerfCache(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public int? GetPerf(int contestId, string handle)
        {
            if (_cache.TryGetValue(contestId, out var contestCache))
            {
                if (contestCache.TryGetValue(handle, out var perf))
                {
                    return perf;
                }
            }

            return null;
        }

        public async Task EnsurePerf(int contestId, string handle, List<Entry> standings, Dictionary<string, int> ratings)
        {
            await EnsureCacheLoaded();
            bool mustSave = false;
            if (!_cache.ContainsKey(contestId))
            {
                _cache.Add(contestId, new());
            }

            if (!_cache[contestId].ContainsKey(handle))
            {
                _cache[contestId][handle] = await CalculatePerformance(handle, standings, ratings);
                mustSave = true;
            }

            if (mustSave)
            {
                await SaveCacheToLocalStorage(contestId);
            }
        }

        private async Task<int> CalculatePerformance(string handle, List<Entry> standings, Dictionary<string, int> ratings)
        {
            int lo = 0, hi = 5000, mid;
            double? rank = null;
            foreach (var entry in standings)
            {
                if (entry.handle == handle)
                {
                    rank = entry.rank;
                }
            }

            if (rank == null)
            {
                throw new($"no rank for {handle}");
            }

            while (lo < hi)
            {
                mid = (lo + hi+1) / 2;
                double left = 0;
                foreach (var entry in standings)
                {
                    left += 1.0 / (1 + Math.Pow(10, (mid - ratings[entry.handle]) / 400.0 ));
                }
                double right = rank.Value - 0.5;
                if (left > right)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid-1;
                }
            }

            //This is to give the UI time to render something on the screen too.
            //Shouldn't actually affect the time much, 5ms isn't the majority.
            await Task.Delay(5);
            return lo;
        }

        private async Task SaveCacheToLocalStorage(int contestId)
        {
            if (_cache.TryGetValue(contestId, out var contestCache))
            {
                var serializedData = JsonSerializer.Serialize(contestCache);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"contestPerf_{contestId}", serializedData);
            }
        }

        private async Task EnsureCacheLoaded()
        {
            if (_initialized)
            {
                return;
            }
            var keys = await _jsRuntime.InvokeAsync<string[]>("eval", "Object.keys(localStorage)");

            foreach (var key in keys)
            {
                // Check if the key starts with "contestPerf_"
                if (key.StartsWith("contestPerf_"))
                {
                    // Extract contestId from the key
                    if (int.TryParse(key.Substring("contestPerf_".Length), out int contestId))
                    {
                        // Retrieve and deserialize the data
                        var cachedData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                        if (!string.IsNullOrEmpty(cachedData))
                        {
                            var contestCache = JsonSerializer.Deserialize<Dictionary<string, int>>(cachedData);
                            if (contestCache != null)
                            {
                                _cache[contestId] = contestCache;
                            }
                        }
                    }
                }
            }
            _initialized = true;
        }
    }
}
