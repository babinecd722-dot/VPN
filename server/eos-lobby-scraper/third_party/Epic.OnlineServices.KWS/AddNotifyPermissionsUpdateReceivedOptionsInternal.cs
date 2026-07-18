using System;

namespace Epic.OnlineServices.KWS;

internal struct AddNotifyPermissionsUpdateReceivedOptionsInternal : ISettable<AddNotifyPermissionsUpdateReceivedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyPermissionsUpdateReceivedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
