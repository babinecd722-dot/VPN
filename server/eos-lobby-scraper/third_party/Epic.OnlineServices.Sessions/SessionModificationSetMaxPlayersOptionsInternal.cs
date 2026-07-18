using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetMaxPlayersOptionsInternal : ISettable<SessionModificationSetMaxPlayersOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxPlayers;

	public void Set(ref SessionModificationSetMaxPlayersOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxPlayers = other.MaxPlayers;
	}

	public void Dispose()
	{
	}
}
