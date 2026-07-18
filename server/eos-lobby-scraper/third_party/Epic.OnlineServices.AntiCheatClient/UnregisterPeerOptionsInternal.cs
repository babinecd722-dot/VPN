using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct UnregisterPeerOptionsInternal : ISettable<UnregisterPeerOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PeerHandle;

	public void Set(ref UnregisterPeerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PeerHandle = other.PeerHandle;
	}

	public void Dispose()
	{
	}
}
