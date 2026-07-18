using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyRequestToJoinRejectedOptionsInternal : ISettable<AddNotifyRequestToJoinRejectedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyRequestToJoinRejectedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
