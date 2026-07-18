using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct AddNotifyClientAuthStatusChangedOptionsInternal : ISettable<AddNotifyClientAuthStatusChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyClientAuthStatusChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
