using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyRequestToJoinResponseReceivedOptionsInternal : ISettable<AddNotifyRequestToJoinResponseReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyRequestToJoinResponseReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
