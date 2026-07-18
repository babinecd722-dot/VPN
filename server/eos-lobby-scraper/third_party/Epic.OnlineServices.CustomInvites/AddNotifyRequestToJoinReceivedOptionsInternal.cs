using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyRequestToJoinReceivedOptionsInternal : ISettable<AddNotifyRequestToJoinReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyRequestToJoinReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
