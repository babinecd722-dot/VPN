using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationDataRecordIdInternal : ISettable<PresenceModificationDataRecordId>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	public void Set(ref PresenceModificationDataRecordId other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
	}
}
