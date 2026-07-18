using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyInviteAcceptedOptionsInternal : ISettable<AddNotifyLobbyInviteAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyInviteAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
