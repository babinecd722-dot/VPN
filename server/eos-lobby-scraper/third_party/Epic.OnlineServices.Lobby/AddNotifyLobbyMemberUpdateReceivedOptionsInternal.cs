using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLobbyMemberUpdateReceivedOptionsInternal : ISettable<AddNotifyLobbyMemberUpdateReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLobbyMemberUpdateReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
