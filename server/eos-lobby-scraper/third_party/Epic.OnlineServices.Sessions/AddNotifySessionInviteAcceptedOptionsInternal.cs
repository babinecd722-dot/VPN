using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifySessionInviteAcceptedOptionsInternal : ISettable<AddNotifySessionInviteAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySessionInviteAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
