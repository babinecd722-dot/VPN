using System;

namespace Epic.OnlineServices.Sessions;

internal struct RejectInviteOptionsInternal : ISettable<RejectInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_InviteId;

	public void Set(ref RejectInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.InviteId, ref m_InviteId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_InviteId);
	}
}
