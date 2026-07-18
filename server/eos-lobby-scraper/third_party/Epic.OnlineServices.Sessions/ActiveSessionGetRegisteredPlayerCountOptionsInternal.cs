using System;

namespace Epic.OnlineServices.Sessions;

internal struct ActiveSessionGetRegisteredPlayerCountOptionsInternal : ISettable<ActiveSessionGetRegisteredPlayerCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref ActiveSessionGetRegisteredPlayerCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
