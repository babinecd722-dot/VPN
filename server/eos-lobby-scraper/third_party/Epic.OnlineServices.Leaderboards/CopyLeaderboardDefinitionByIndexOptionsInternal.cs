using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardDefinitionByIndexOptionsInternal : ISettable<CopyLeaderboardDefinitionByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_LeaderboardIndex;

	public void Set(ref CopyLeaderboardDefinitionByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_LeaderboardIndex = other.LeaderboardIndex;
	}

	public void Dispose()
	{
	}
}
