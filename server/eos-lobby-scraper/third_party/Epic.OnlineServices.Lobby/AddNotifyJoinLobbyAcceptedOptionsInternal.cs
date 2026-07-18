using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyJoinLobbyAcceptedOptionsInternal : ISettable<AddNotifyJoinLobbyAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyJoinLobbyAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
