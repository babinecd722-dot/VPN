using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct AddNotifyMessageToPeerOptionsInternal : ISettable<AddNotifyMessageToPeerOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyMessageToPeerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
