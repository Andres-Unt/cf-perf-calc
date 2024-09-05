using System.Net.Http;
using System.Net.Http.Json;
using CfPerfCalc.Services;
using CfPerfCalc.Services.FormulaOptimizer;

public class FormulaOptimizationService(HttpClient httpClient, CfApiClient cfApiClient, RatingCache ratingCache)
{
    private Dictionary<int, List<RatingChange>> ratingChangeCache = [];
    public async Task<OptimizationResult> CalculateForContest(int contestId, double elitismValue)
    {
        var ratingChanges = await EnsureRatingChanges(contestId);
        var realPerf = ratingChanges.ToDictionary(change => change.handle,
            change => change.oldRating + (change.newRating-change.oldRating) * 4);
        var oldRating = ratingChanges.ToDictionary(change => change.handle,
            change => change.oldRating);
        var handlesNeeded = realPerf.Keys.ToHashSet();
        await cfApiClient.EnsureRatings(handlesNeeded.ToList());
        var currentRatings = await cfApiClient.GetRatedListAsync();
        var standings = (await cfApiClient.GetContestStandingsAsync(contestId)).Where(entry => handlesNeeded.Contains(entry.handle) && entry.type == "CONTESTANT").ToList();
        CfApiClient.FixRanksInStandings(standings);
        double squareError = 0;
        var estimation = new Dictionary<string, int>();
        var count = 0;
        foreach (var entry in standings.Take(100))
        {
            ++count;
            var handle = entry.handle;
            estimation[handle] = PerfCache.CalculatePerformance(handle, standings, currentRatings, elitismValue);
            // estimation[handle] = PerfCache.CalculatePerformance(handle, standings, oldRating, elitismValue);
            var est = estimation[handle];
            var real = realPerf[handle];
            squareError += Math.Pow(est - real, 2);
        }

        squareError /= count;

        return new OptimizationResult
        {
            ContestId = contestId,
            ElitismValue = elitismValue,
            SquareError = Math.Round(squareError, 2)
        };
    }

    private async Task<List<RatingChange>> EnsureRatingChanges(int contestId)
    {
        if (ratingChangeCache.TryGetValue(contestId, out var list))
        {
            return list;
        }
        
        await Task.Delay(2100);
        var ratingChanges = await httpClient.GetFromJsonAsync<RatingChanges>(
            $"https://codeforces.com/api/contest.ratingChanges?contestId={contestId}");
        
        if (ratingChanges == null || ratingChanges.result.Count == 0)
        {
            throw new Exception("No rating changes found for contest");
        }

        ratingChangeCache[contestId] = ratingChanges.result;
        return ratingChangeCache[contestId];
    }
}
