using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct UnregisterClientOptionsInternal : ISettable<UnregisterClientOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	public void Set(ref UnregisterClientOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
	}

	public void Dispose()
	{
	}
}
