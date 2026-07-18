using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyLeaveLobbyRequestedOptionsInternal : ISettable<AddNotifyLeaveLobbyRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLeaveLobbyRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
