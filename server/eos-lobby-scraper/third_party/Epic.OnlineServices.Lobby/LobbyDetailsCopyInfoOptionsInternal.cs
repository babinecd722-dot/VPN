using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsCopyInfoOptionsInternal : ISettable<LobbyDetailsCopyInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref LobbyDetailsCopyInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
