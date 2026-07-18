using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct AddNotifyUserLoginStatusChangedOptionsInternal : ISettable<AddNotifyUserLoginStatusChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyUserLoginStatusChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
