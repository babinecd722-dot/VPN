using System;

namespace Epic.OnlineServices.Lobby;

internal struct RejectInviteOptionsInternal : ISettable<RejectInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_InviteId;

	private IntPtr m_LocalUserId;

	public void Set(ref RejectInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.InviteId, ref m_InviteId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_InviteId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
