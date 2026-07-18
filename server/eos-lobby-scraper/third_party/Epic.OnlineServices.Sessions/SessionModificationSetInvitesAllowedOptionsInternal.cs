using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetInvitesAllowedOptionsInternal : ISettable<SessionModificationSetInvitesAllowedOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_InvitesAllowed;

	public void Set(ref SessionModificationSetInvitesAllowedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.InvitesAllowed, ref m_InvitesAllowed);
	}

	public void Dispose()
	{
	}
}
