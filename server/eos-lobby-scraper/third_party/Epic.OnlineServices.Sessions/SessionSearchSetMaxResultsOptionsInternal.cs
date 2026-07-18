using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchSetMaxResultsOptionsInternal : ISettable<SessionSearchSetMaxResultsOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxSearchResults;

	public void Set(ref SessionSearchSetMaxResultsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxSearchResults = other.MaxSearchResults;
	}

	public void Dispose()
	{
	}
}
