using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardDefinitionByLeaderboardIdOptionsInternal : ISettable<CopyLeaderboardDefinitionByLeaderboardIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LeaderboardId;

	public void Set(ref CopyLeaderboardDefinitionByLeaderboardIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.LeaderboardId, ref m_LeaderboardId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LeaderboardId);
	}
}
