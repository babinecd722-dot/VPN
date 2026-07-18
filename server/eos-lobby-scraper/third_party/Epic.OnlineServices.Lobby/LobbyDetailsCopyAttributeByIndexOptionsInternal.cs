using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsCopyAttributeByIndexOptionsInternal : ISettable<LobbyDetailsCopyAttributeByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_AttrIndex;

	public void Set(ref LobbyDetailsCopyAttributeByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_AttrIndex = other.AttrIndex;
	}

	public void Dispose()
	{
	}
}
