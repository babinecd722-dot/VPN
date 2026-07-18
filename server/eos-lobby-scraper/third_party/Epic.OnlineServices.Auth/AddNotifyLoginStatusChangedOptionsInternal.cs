using System;

namespace Epic.OnlineServices.Auth;

internal struct AddNotifyLoginStatusChangedOptionsInternal : ISettable<AddNotifyLoginStatusChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyLoginStatusChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
