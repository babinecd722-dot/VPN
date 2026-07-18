using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyInviteRejectedOptionsInternal : ISettable<AddNotifyLobbyInviteRejectedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyInviteRejectedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
