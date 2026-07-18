using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct AddNotifyCustomInviteRejectedOptionsInternal : ISettable<AddNotifyCustomInviteRejectedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyCustomInviteRejectedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
