using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct SetClientNetworkStateOptionsInternal : ISettable<SetClientNetworkStateOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private int m_IsNetworkActive;

	public void Set(ref SetClientNetworkStateOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
		Helper.Set(other.IsNetworkActive, ref m_IsNetworkActive);
	}

	public void Dispose()
	{
	}
}
