using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsGetMemberCountOptionsInternal : ISettable<LobbyDetailsGetMemberCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref LobbyDetailsGetMemberCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
