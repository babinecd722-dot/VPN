using System;

namespace Epic.OnlineServices.Lobby;

internal struct CreateLobbySearchOptionsInternal : ISettable<CreateLobbySearchOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxResults;

	public void Set(ref CreateLobbySearchOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxResults = other.MaxResults;
	}

	public void Dispose()
	{
	}
}
