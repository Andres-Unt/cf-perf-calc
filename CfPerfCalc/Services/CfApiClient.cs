using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace CfPerfCalc.Services
{
    public class CfApiClient(JSInterop jsInterop, HttpClient httpClient, RatingCache ratingCache)
    {
        private Dictionary<int, List<Entry>> _cachedStandings = new();
        public async Task<Dictionary<string,int>> GetRatedListAsync()
        {
            var cached = await ratingCache.GetRatedListAsync();
            if (cached != null)
            {
                return cached;
            }
            var url = "https://codeforces.com/api/user.ratedList?activeOnly=true&includeRetired=true";
            var ratedList = await httpClient.GetFromJsonAsync<RatedList>(url);
            ratedList.result = ratedList.result.Where(user => user.rating >= 1600).ToList();
            var dict = ratedList.result.ToDictionary(user => user.handle, user => user.rating);
            await ratingCache.Update(dict);
            var result = await ratingCache.GetRatedListAsync();
            return result ?? throw new("Cache is dumb.");
        }
       public async Task<List<Entry>> GetContestStandingsAsync(int contestId)
        {
            // Check if data is cached in memory
            if (_cachedStandings.ContainsKey(contestId))
            {
                return _cachedStandings[contestId];
            }

            // Check if data is cached in localStorage
            var cachedData = await jsInterop.InvokeAsync<string>("localStorage.getItem", $"cfContestStandings_{contestId}");
            var cachedTimestamp = await jsInterop.InvokeAsync<string>("localStorage.getItem", $"cfContestStandingsTimestamp_{contestId}");

            if (!string.IsNullOrEmpty(cachedData) && !string.IsNullOrEmpty(cachedTimestamp))
            {
                var cacheDate = DateTime.Parse(cachedTimestamp);
                if (cacheDate > DateTime.Now.AddDays(-7))
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var cachedStandings = JsonSerializer.Deserialize<List<Entry>>(cachedData);
                    _cachedStandings[contestId] = cachedStandings;
                    var elapsed = watch.Elapsed;
                    return cachedStandings;
                }
            }

            // Fetch contest standings from API

            var url = $"https://codeforces.com/api/contest.standings?contestId={contestId}&showUnofficial=true";
            var response = await httpClient.GetFromJsonAsync<ContestStandingsResponse>(url);

            if (response.status != "OK")
            {
                throw new HttpRequestException("Failed to fetch contest standings");
            }

            // Fetch user ratings (already implemented in previous method)
            var ratedList = await GetRatedListAsync();

            var filteredRows = response.result.rows
                .Where(row => 
                {
                    if (row.party is null)
                    {
                        throw new Exception("party is null");
                    }
                    if (row.party.members.Count != 1)
                    {
                        return false;
                    }
                    switch (row.party.participantType)
                    {
                        case "CONTESTANT":
                        case "OUT_OF_COMPETITION":
                        case "VIRTUAL":
                            break;
                        default:
                            return false;
                    }
                    // var member = row.party.members.First();
                    // var handle = member.handle;
                    // return usersKept.Contains(handle);
                    return true;
                })
                .OrderBy(row => row.rank)
                .Take(2000)
                .ToList();

            // Transform to sorted list of tuples (participantType, handle)
            var standings = filteredRows.Select(row =>
                new Entry(row)
                
            ).ToList();

            // Cache the result in memory
            _cachedStandings[contestId] = standings;

            // Cache the result in localStorage
            var serializedData = JsonSerializer.Serialize(standings);
            await jsInterop.InvokeVoidAsync("localStorage.setItem", $"cfContestStandings_{contestId}", serializedData);
            await jsInterop.InvokeVoidAsync("localStorage.setItem", $"cfContestStandingsTimestamp_{contestId}", DateTime.Now.ToString());

            return standings;
        }

        public async Task EnsureRatings(IList<string> handles)
        {
            string? singleHandle = handles.Count == 1 ? handles[0] : null;
            var ratingDict = await GetRatedListAsync();
            var handlesQuery = string.Join(';', handles);
            handlesQuery = Uri.EscapeDataString(handlesQuery);
            var url = $"https://codeforces.com/api/user.info?handles={handlesQuery}&checkHistoricHandles=true";
            var response = await httpClient.GetFromJsonAsync<RatedList>(url);
            List<string> stillMissing = [];
            if (response?.status == "OK" && response.result != null)
            {
                var nowGot = new HashSet<string>();
                foreach (var user in response.result)
                {
                    ratingDict[user.handle] = user.rating;
                    nowGot.Add(user.handle);
                    if (singleHandle != null)
                    {
                        ratingDict[singleHandle] = user.rating;
                        nowGot.Add(singleHandle);
                    }
                }

                foreach (var user in handles)
                {
                    if (!nowGot.Contains(user))
                    {
                        stillMissing.Add(user);
                    }
                }

                await ratingCache.Update(ratingDict);
            }
            else
            {
                throw new HttpRequestException($"Failed to fetch ratings for handles: {handlesQuery}");
            }

            foreach (var single in stillMissing)
            {
                await EnsureRatings([single]);
            }
        }
    }
}
