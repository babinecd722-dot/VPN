using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchSetMaxResultsOptionsInternal : ISettable<LobbySearchSetMaxResultsOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxResults;

	public void Set(ref LobbySearchSetMaxResultsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxResults = other.MaxResults;
	}

	public void Dispose()
	{
	}
}
