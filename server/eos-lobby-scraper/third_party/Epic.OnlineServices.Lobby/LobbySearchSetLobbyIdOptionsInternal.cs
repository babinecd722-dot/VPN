using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchSetLobbyIdOptionsInternal : ISettable<LobbySearchSetLobbyIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	public void Set(ref LobbySearchSetLobbyIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.LobbyId, ref m_LobbyId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
	}
}
