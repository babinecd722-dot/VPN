using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct CopyLeaderboardRecordByIndexOptionsInternal : ISettable<CopyLeaderboardRecordByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_LeaderboardRecordIndex;

	public void Set(ref CopyLeaderboardRecordByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		m_LeaderboardRecordIndex = other.LeaderboardRecordIndex;
	}

	public void Dispose()
	{
	}
}
