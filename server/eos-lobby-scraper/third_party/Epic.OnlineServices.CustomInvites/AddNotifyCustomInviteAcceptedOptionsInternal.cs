using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyCustomInviteAcceptedOptionsInternal : ISettable<AddNotifyCustomInviteAcceptedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyCustomInviteAcceptedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
