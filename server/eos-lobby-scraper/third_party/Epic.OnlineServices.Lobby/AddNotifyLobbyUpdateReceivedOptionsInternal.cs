using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyUpdateReceivedOptionsInternal : ISettable<AddNotifyLobbyUpdateReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyUpdateReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
