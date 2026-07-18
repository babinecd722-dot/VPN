using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchGetSearchResultCountOptionsInternal : ISettable<LobbySearchGetSearchResultCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref LobbySearchGetSearchResultCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
