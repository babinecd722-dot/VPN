using System;

namespace Epic.OnlineServices.Presence;

internal struct SetPresenceOptionsInternal : ISettable<SetPresenceOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_PresenceModificationHandle;

	public void Set(ref SetPresenceOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.PresenceModificationHandle, ref m_PresenceModificationHandle);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_PresenceModificationHandle);
	}
}
