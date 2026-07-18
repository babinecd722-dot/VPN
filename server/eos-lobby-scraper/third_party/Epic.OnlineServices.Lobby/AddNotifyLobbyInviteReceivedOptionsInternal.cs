using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyInviteReceivedOptionsInternal : ISettable<AddNotifyLobbyInviteReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyInviteReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
