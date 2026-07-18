using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddNotifyPeerActionRequiredOptionsInternal : ISettable<AddNotifyPeerActionRequiredOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyPeerActionRequiredOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
