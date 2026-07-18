using System;

namespace Epic.OnlineServices.Leaderboards;

public struct Definition
{
	public Utf8String LeaderboardId { get; set; }

	public Utf8String StatName { get; set; }

	public LeaderboardAggregation Aggregation { get; set; }

	public DateTimeOffset? StartTime { get; set; }

	public DateTimeOffset? EndTime { get; set; }
}
