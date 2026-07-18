using System;

namespace Epic.OnlineServices.Sessions;

internal struct SendInviteOptionsInternal : ISettable<SendInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public void Set(ref SendInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
