using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct DefinitionInternal : IGettable<Definition>
{
	private int m_ApiVersion;

	private IntPtr m_LeaderboardId;

	private IntPtr m_StatName;

	private LeaderboardAggregation m_Aggregation;

	private long m_StartTime;

	private long m_EndTime;

	public void Get(out Definition other)
	{
		other = default(Definition);
		Helper.Get(m_LeaderboardId, out Utf8String to);
		other.LeaderboardId = to;
		Helper.Get(m_StatName, out Utf8String to2);
		other.StatName = to2;
		other.Aggregation = m_Aggregation;
		Helper.Get(m_StartTime, out DateTimeOffset? to3);
		other.StartTime = to3;
		Helper.Get(m_EndTime, out DateTimeOffset? to4);
		other.EndTime = to4;
	}
}
