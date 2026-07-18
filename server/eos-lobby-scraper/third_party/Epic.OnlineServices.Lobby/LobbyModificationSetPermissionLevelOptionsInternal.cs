using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationSetPermissionLevelOptionsInternal : ISettable<LobbyModificationSetPermissionLevelOptions>, IDisposable
{
	private int m_ApiVersion;

	private LobbyPermissionLevel m_PermissionLevel;

	public void Set(ref LobbyModificationSetPermissionLevelOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PermissionLevel = other.PermissionLevel;
	}

	public void Dispose()
	{
	}
}
