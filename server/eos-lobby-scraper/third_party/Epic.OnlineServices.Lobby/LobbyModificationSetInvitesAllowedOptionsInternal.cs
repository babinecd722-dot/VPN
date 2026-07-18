using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationSetInvitesAllowedOptionsInternal : ISettable<LobbyModificationSetInvitesAllowedOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_InvitesAllowed;

	public void Set(ref LobbyModificationSetInvitesAllowedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.InvitesAllowed, ref m_InvitesAllowed);
	}

	public void Dispose()
	{
	}
}
