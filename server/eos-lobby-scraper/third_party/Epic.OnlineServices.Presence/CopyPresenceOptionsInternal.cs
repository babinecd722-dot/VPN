using System;

namespace Epic.OnlineServices.Presence;

internal struct CopyPresenceOptionsInternal : ISettable<CopyPresenceOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public void Set(ref CopyPresenceOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
