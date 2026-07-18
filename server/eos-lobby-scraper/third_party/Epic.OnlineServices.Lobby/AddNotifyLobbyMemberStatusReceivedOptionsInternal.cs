using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyMemberStatusReceivedOptionsInternal : ISettable<AddNotifyLobbyMemberStatusReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyMemberStatusReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
