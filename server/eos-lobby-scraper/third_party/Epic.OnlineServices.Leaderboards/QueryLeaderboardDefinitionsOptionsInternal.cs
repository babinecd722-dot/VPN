using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct QueryLeaderboardDefinitionsOptionsInternal : ISettable<QueryLeaderboardDefinitionsOptions>, IDisposable
{
	private int m_ApiVersion;

	private long m_StartTime;

	private long m_EndTime;

	private IntPtr m_LocalUserId;

	public void Set(ref QueryLeaderboardDefinitionsOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.StartTime, ref m_StartTime);
		Helper.Set(other.EndTime, ref m_EndTime);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
