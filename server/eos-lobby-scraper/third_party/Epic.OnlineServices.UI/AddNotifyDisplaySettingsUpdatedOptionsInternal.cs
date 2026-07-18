using System;

namespace Epic.OnlineServices.UI;

internal struct AddNotifyDisplaySettingsUpdatedOptionsInternal : ISettable<AddNotifyDisplaySettingsUpdatedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyDisplaySettingsUpdatedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
