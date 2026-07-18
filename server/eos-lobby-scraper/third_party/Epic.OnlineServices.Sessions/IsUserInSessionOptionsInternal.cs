using System;

namespace Epic.OnlineServices.Sessions;

internal struct IsUserInSessionOptionsInternal : ISettable<IsUserInSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_TargetUserId;

	public void Set(ref IsUserInSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_TargetUserId);
	}
}
