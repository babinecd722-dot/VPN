using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifySessionInviteRejectedOptionsInternal : ISettable<AddNotifySessionInviteRejectedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySessionInviteRejectedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
