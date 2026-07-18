using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifySendLobbyNativeInviteRequestedOptionsInternal : ISettable<AddNotifySendLobbyNativeInviteRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySendLobbyNativeInviteRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
