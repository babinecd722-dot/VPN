using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchGetSearchResultCountOptionsInternal : ISettable<SessionSearchGetSearchResultCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref SessionSearchGetSearchResultCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
