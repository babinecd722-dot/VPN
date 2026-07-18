using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddNotifyPeerAuthStatusChangedOptionsInternal : ISettable<AddNotifyPeerAuthStatusChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyPeerAuthStatusChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
