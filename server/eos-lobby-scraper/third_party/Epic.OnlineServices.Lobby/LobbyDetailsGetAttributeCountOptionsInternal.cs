using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsGetAttributeCountOptionsInternal : ISettable<LobbyDetailsGetAttributeCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref LobbyDetailsGetAttributeCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
