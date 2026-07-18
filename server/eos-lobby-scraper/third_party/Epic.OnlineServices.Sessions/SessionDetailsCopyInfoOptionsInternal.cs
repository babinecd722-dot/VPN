using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsCopyInfoOptionsInternal : ISettable<SessionDetailsCopyInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref SessionDetailsCopyInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
