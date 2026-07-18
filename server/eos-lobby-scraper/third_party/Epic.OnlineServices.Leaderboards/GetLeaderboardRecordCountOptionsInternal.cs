using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct GetLeaderboardRecordCountOptionsInternal : ISettable<GetLeaderboardRecordCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetLeaderboardRecordCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
