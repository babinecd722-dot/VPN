using System;

namespace Epic.OnlineServices.Sessions;

internal struct ActiveSessionGetRegisteredPlayerByIndexOptionsInternal : ISettable<ActiveSessionGetRegisteredPlayerByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_PlayerIndex;

	public void Set(ref ActiveSessionGetRegisteredPlayerByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PlayerIndex = other.PlayerIndex;
	}

	public void Dispose()
	{
	}
}
