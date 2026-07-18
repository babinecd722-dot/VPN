using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct QueryLeaderboardRanksOptionsInternal : ISettable<QueryLeaderboardRanksOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LeaderboardId;

	private IntPtr m_LocalUserId;

	public void Set(ref QueryLeaderboardRanksOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.LeaderboardId, ref m_LeaderboardId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LeaderboardId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
