namespace CfPerfCalc.Services;

public class User
{
    public string handle { get; set; }
    public int rating { get; set; }
    // public int maxRating { get; set; }
    // public string titlePhoto { get; set; }
    // public string country { get; set; }
    // public string rank { get; set; }
    // public string maxRank { get; set; }
}
public class RatedList
{
    public string status { get; set; }
    public List<User> result { get; set; }
}