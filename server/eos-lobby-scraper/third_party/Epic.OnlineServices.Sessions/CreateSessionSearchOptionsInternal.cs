using System;

namespace Epic.OnlineServices.Sessions;

internal struct CreateSessionSearchOptionsInternal : ISettable<CreateSessionSearchOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxSearchResults;

	public void Set(ref CreateSessionSearchOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxSearchResults = other.MaxSearchResults;
	}

	public void Dispose()
	{
	}
}
