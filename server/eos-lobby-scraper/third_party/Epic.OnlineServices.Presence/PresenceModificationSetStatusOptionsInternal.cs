using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetStatusOptionsInternal : ISettable<PresenceModificationSetStatusOptions>, IDisposable
{
	private int m_ApiVersion;

	private Status m_Status;

	public void Set(ref PresenceModificationSetStatusOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_Status = other.Status;
	}

	public void Dispose()
	{
	}
}
