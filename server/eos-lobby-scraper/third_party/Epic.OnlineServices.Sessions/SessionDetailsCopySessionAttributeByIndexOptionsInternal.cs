using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsCopySessionAttributeByIndexOptionsInternal : ISettable<SessionDetailsCopySessionAttributeByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_AttrIndex;

	public void Set(ref SessionDetailsCopySessionAttributeByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_AttrIndex = other.AttrIndex;
	}

	public void Dispose()
	{
	}
}
