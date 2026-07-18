using System;

namespace Epic.OnlineServices.Sessions;

internal struct AddNotifySendSessionNativeInviteRequestedOptionsInternal : ISettable<AddNotifySendSessionNativeInviteRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySendSessionNativeInviteRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
