using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyCustomInviteReceivedOptionsInternal : ISettable<AddNotifyCustomInviteReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyCustomInviteReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
