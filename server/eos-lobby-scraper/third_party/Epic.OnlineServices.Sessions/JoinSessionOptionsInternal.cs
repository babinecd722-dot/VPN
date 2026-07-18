using System;

namespace Epic.OnlineServices.Sessions;

internal struct JoinSessionOptionsInternal : ISettable<JoinSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_SessionHandle;

	private IntPtr m_LocalUserId;

	private int m_PresenceEnabled;

	public void Set(ref JoinSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.SessionName, ref m_SessionName);
		Helper.Set((Handle)other.SessionHandle, ref m_SessionHandle);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.PresenceEnabled, ref m_PresenceEnabled);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_SessionHandle);
		Helper.Dispose(ref m_LocalUserId);
	}
}
