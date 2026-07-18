namespace Epic.OnlineServices.Leaderboards;

public struct UserScoresQueryStatInfo
{
	public Utf8String StatName { get; set; }

	public LeaderboardAggregation Aggregation { get; set; }
}
