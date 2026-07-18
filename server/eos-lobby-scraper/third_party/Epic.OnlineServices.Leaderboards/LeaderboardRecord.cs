namespace Epic.OnlineServices.Leaderboards;

public struct LeaderboardRecord
{
	public ProductUserId UserId { get; set; }

	public uint Rank { get; set; }

	public int Score { get; set; }

	public Utf8String UserDisplayName { get; set; }
}
