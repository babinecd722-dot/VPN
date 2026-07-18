using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyRequestToJoinAcceptedOptionsInternal : ISettable<AddNotifyRequestToJoinAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyRequestToJoinAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
