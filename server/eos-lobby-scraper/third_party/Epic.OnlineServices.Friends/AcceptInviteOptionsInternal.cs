using System;

namespace Epic.OnlineServices.Friends;

internal struct AcceptInviteOptionsInternal : ISettable<AcceptInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public void Set(ref AcceptInviteOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
