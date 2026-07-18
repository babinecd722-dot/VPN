using System;

namespace Epic.OnlineServices.UI;

internal struct SetDisplayPreferenceOptionsInternal : ISettable<SetDisplayPreferenceOptions>, IDisposable
{
	private int m_ApiVersion;

	private NotificationLocation m_NotificationLocation;

	public void Set(ref SetDisplayPreferenceOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_NotificationLocation = other.NotificationLocation;
	}

	public void Dispose()
	{
	}
}
