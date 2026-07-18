using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct UserScoresQueryStatInfoInternal : ISettable<UserScoresQueryStatInfo>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_StatName;

	private LeaderboardAggregation m_Aggregation;

	public void Set(ref UserScoresQueryStatInfo other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.StatName, ref m_StatName);
		m_Aggregation = other.Aggregation;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_StatName);
	}
}
