using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifySessionInviteReceivedOptionsInternal : ISettable<AddNotifySessionInviteReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySessionInviteReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
