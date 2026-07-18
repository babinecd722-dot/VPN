using System;

namespace Epic.OnlineServices.Sessions;

internal struct ActiveSessionCopyInfoOptionsInternal : ISettable<ActiveSessionCopyInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref ActiveSessionCopyInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
