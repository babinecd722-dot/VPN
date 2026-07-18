using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchCopySearchResultByIndexOptionsInternal : ISettable<LobbySearchCopySearchResultByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_LobbyIndex;

	public void Set(ref LobbySearchCopySearchResultByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_LobbyIndex = other.LobbyIndex;
	}

	public void Dispose()
	{
	}
}
