using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct EndSessionOptionsInternal : ISettable<EndSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref EndSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
