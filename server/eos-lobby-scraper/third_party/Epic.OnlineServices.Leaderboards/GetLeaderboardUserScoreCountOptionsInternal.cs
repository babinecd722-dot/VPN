using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct GetLeaderboardUserScoreCountOptionsInternal : ISettable<GetLeaderboardUserScoreCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_StatName;

	public void Set(ref GetLeaderboardUserScoreCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.StatName, ref m_StatName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_StatName);
	}
}
