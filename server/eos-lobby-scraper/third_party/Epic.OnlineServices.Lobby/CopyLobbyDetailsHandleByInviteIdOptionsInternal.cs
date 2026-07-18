using System;

namespace Epic.OnlineServices.Lobby;

internal struct CopyLobbyDetailsHandleByInviteIdOptionsInternal : ISettable<CopyLobbyDetailsHandleByInviteIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_InviteId;

	public void Set(ref CopyLobbyDetailsHandleByInviteIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.InviteId, ref m_InviteId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_InviteId);
	}
}
