using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifySendCustomNativeInviteRequestedOptionsInternal : ISettable<AddNotifySendCustomNativeInviteRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifySendCustomNativeInviteRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
