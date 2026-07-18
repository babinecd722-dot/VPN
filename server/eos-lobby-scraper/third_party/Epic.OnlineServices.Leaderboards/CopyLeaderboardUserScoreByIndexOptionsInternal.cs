using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardUserScoreByIndexOptionsInternal : ISettable<CopyLeaderboardUserScoreByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_LeaderboardUserScoreIndex;

	private IntPtr m_StatName;

	public void Set(ref CopyLeaderboardUserScoreByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_LeaderboardUserScoreIndex = other.LeaderboardUserScoreIndex;
		Helper.Set(other.StatName, ref m_StatName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_StatName);
	}
}
