using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct GetLeaderboardDefinitionCountOptionsInternal : ISettable<GetLeaderboardDefinitionCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetLeaderboardDefinitionCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
