using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsGetLobbyOwnerOptionsInternal : ISettable<LobbyDetailsGetLobbyOwnerOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref LobbyDetailsGetLobbyOwnerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
