using System;

namespace Epic.OnlineServices.Presence;

internal struct AddNotifyOnPresenceChangedOptionsInternal : ISettable<AddNotifyOnPresenceChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyOnPresenceChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
