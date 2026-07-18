using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct AddNotifyClientActionRequiredOptionsInternal : ISettable<AddNotifyClientActionRequiredOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyClientActionRequiredOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
