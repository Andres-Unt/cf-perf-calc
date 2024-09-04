namespace CfPerfCalc.Services;

public class Entry
{
    public string handle { get; set; }
    public string type { get; set; }
    public double points { get; set; }
    public int penalty { get; set; }
    public double rank { get; set; }

    public Entry()
    {
    }
    public Entry(RanklistRow row)
    {
        handle = row.party.members[0].handle;
        type = row.party.participantType;
        points = row.points;
        penalty = row.penalty;
        rank = row.rank;
    }
}
public class Member
{
    public string handle { get; set; }
}

public class Party
{
    public string participantType { get; set; }
    public List<Member> members { get; set; }
}
public class RanklistRow
{
    public int rank { get; set; }
    public Party party { get; set; }
    public double points { get; set; }
    public int penalty { get; set; }
}

public class ContestStandings
{
    public List<RanklistRow> rows { get; set; }
}

public class ContestStandingsResponse
{
    public string status { get; set; }
    public ContestStandings result { get; set; }
}