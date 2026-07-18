using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddNotifyMessageToServerOptionsInternal : ISettable<AddNotifyMessageToServerOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyMessageToServerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
