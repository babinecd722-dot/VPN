using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsGetSessionAttributeCountOptionsInternal : ISettable<SessionDetailsGetSessionAttributeCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref SessionDetailsGetSessionAttributeCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
