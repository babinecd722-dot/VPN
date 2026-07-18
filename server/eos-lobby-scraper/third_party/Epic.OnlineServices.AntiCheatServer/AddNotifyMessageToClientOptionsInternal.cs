using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct AddNotifyMessageToClientOptionsInternal : ISettable<AddNotifyMessageToClientOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyMessageToClientOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
