using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchCopySearchResultByIndexOptionsInternal : ISettable<SessionSearchCopySearchResultByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_SessionIndex;

	public void Set(ref SessionSearchCopySearchResultByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_SessionIndex = other.SessionIndex;
	}

	public void Dispose()
	{
	}
}
